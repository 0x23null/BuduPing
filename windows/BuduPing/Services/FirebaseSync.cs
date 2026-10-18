using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Text.Json;
using BuduPing.Core;

namespace BuduPing.Services;

/// <summary>
/// Đồng bộ Firebase Realtime Database bằng REST + streaming SSE (KHÔNG dùng Firebase SDK,
/// KHÔNG polling). Nghe push tại /pairs/{pairId}/inbox, heartbeat presence mỗi 20s.
/// </summary>
public sealed class FirebaseSync : IDisposable
{
    private const string BaseUrl = "https://buduping-default-rtdb.asia-southeast1.firebasedatabase.app/";
    private const int MaxSeenIds = 500;

    private readonly string _pairId;
    private readonly HttpClient _stream; // timeout vô hạn cho SSE
    private readonly HttpClient _rest;   // cho PUT/DELETE nhanh
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly Queue<string> _seenOrder = new();

    public event Action<PingEvent>? PingReceived;

    public FirebaseSync(string pairId)
    {
        _pairId = pairId;
        _stream = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = Timeout.InfiniteTimeSpan };
        _stream.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        _rest = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    }

    public void Start()
    {
        if (_loopTask != null) return;
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => StreamLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _stream.CancelPendingRequests(); } catch { } // đánh thức ReadLineAsync đang block
        _loopTask = null;
    }

    /// <summary>Vòng lặp SSE: giữ 1 kết nối GET mở, Firebase push event xuống. Tự reconnect với backoff.</summary>
    private async Task StreamLoopAsync(CancellationToken ct)
    {
        int delayMs = 1000;
        string url = $"pairs/{_pairId}/inbox.json";

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var resp = await _stream.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                                             .ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                delayMs = 1000; // kết nối OK → reset backoff

                using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? eventName = null;
                while (!ct.IsCancellationRequested)
                {
                    // ReadLineAsync: block tới khi server push dòng mới / đóng kết nối.
                    // Stop() gọi CancelPendingRequests() để đánh thức khi thoát.
                    string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break; // server đóng → reconnect

                    if (line.StartsWith("event:", StringComparison.Ordinal))
                        eventName = line["event:".Length..].Trim();
                    else if (line.StartsWith("data:", StringComparison.Ordinal))
                    {
                        string data = line["data:".Length..].Trim();
                        HandleSseEvent(eventName, data);
                        eventName = null;
                    }
                    // dòng trống = hết 1 message SSE
                }
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                // lỗi mạng / server → chờ backoff rồi nối lại (1s → 2s → 4s … tối đa 30s)
                try { await Task.Delay(delayMs, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                delayMs = Math.Min(delayMs * 2, 30_000);
            }
        }
    }

    private void HandleSseEvent(string? eventName, string data)
    {
        // keep-alive / cancel / auth_revoked: không có event để hiện
        if (eventName is not ("put" or "patch")) return;
        if (string.IsNullOrEmpty(data) || data == "null") return;

        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            if (!root.TryGetProperty("path", out var pathEl) ||
                !root.TryGetProperty("data", out var dataEl))
                return;

            string path = pathEl.GetString() ?? "";
            if (dataEl.ValueKind == JsonValueKind.Null) return; // event bị xoá → bỏ qua

            if (eventName == "put")
            {
                if (path == "/")
                {
                    // snapshot đầy đủ inbox: { eventId: {...}, ... }
                    if (dataEl.ValueKind == JsonValueKind.Object)
                        foreach (var prop in dataEl.EnumerateObject())
                            TryRaisePing(prop.Value, prop.Name);
                }
                else
                {
                    // path = "/{eventId}", data = 1 event
                    string id = path.Trim('/');
                    if (dataEl.ValueKind == JsonValueKind.Object)
                        TryRaisePing(dataEl, id);
                }
            }
            else // patch: { eventId: {...}, ... }
            {
                if (dataEl.ValueKind == JsonValueKind.Object)
                    foreach (var prop in dataEl.EnumerateObject())
                        TryRaisePing(prop.Value, prop.Name);
            }
        }
        catch { /* JSON lạ → bỏ qua, giữ stream sống */ }
    }

    private void TryRaisePing(JsonElement el, string fallbackId)
    {
        try
        {
            // Id chuẩn để dedupe + xoá inbox PHẢI là Firebase push key (fallbackId:
            // prop.Name của snapshot / đuôi path của put) — KHÔNG dùng field "id" (UUID)
            // bên trong body, vì DELETE /inbox/{uuid} không trúng node nào cả.
            string innerId = el.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? ""
                : "";
            string id = !string.IsNullOrEmpty(fallbackId) ? fallbackId : innerId;
            if (string.IsNullOrEmpty(id)) return;

            lock (_seen)
            {
                if (!_seen.Add(id)) return; // dedupe theo id
                _seenOrder.Enqueue(id);
                while (_seenOrder.Count > MaxSeenIds) _seen.Remove(_seenOrder.Dequeue());
            }

            static string Str(JsonElement e, string name)
                => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString() ?? "" : "";

            var ping = new PingEvent
            {
                Id = id,
                Type = Str(el, "type") is string t && t.Length > 0 ? t : "love",
                Text = Str(el, "text"),
                From = Str(el, "from"),
                Ts = el.TryGetProperty("ts", out var ts) && ts.ValueKind == JsonValueKind.Number
                     ? ts.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
            PingReceived?.Invoke(ping);
        }
        catch { /* event hỏng → bỏ qua */ }
    }

    /// <summary>Xoá event khỏi inbox sau khi đã hiện (fire-and-forget, không block UI).</summary>
    public void DeleteFromInbox(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;
        _ = Task.Run(async () =>
        {
            try { await _rest.DeleteAsync($"pairs/{_pairId}/inbox/{eventId}.json").ConfigureAwait(false); }
            catch { }
        });
    }

    /// <summary>Ghi nhịp presence: { online: true, ts }. Điện thoại đọc để hiện chấm xanh.</summary>
    public async Task PutPresenceAsync(CancellationToken ct = default)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                online = true,
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _rest.PutAsync($"pairs/{_pairId}/presence.json", content, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }
        catch { /* mạng lỗi → lần heartbeat sau thử lại */ }
    }

    /// <summary>Thoát hẳn: xoá presence để điện thoại thấy chấm xám.</summary>
    public async Task DeletePresenceAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _rest.DeleteAsync($"pairs/{_pairId}/presence.json", cts.Token).ConfigureAwait(false);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _stream.Dispose();
        _rest.Dispose();
        _cts?.Dispose();
    }
}
