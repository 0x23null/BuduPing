using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using BuduPing.Core;

namespace BuduPing.Services;

/// <summary>One SSE inbox stream and one serialized completion writer. No inbox polling.</summary>
public sealed class FirebaseSync : IDisposable
{
    private const string BaseUrl = "https://buduping-default-rtdb.asia-southeast1.firebasedatabase.app/";
    private readonly string _pairId;
    private readonly HttpClient _stream = new() { BaseAddress = new Uri(BaseUrl), Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _rest = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    private readonly CancellationTokenSource _cts = new();
    private readonly RecentEventIds _seen = new();
    private readonly Channel<(PingEvent Ping, string Status)> _completed = Channel.CreateUnbounded<(PingEvent, string)>(
        new UnboundedChannelOptions { SingleReader = true });
    private bool _started, _disposed;
    private int _deferred;
    public event Action<PingEvent>? PingReceived;

    public FirebaseSync(string pairId)
    {
        _pairId = pairId;
        _stream.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _ = StreamLoopAsync(_cts.Token);
        _ = CompletionLoopAsync(_cts.Token);
    }

    public void Stop() { _cts.Cancel(); _stream.CancelPendingRequests(); _completed.Writer.TryComplete(); }

    private async Task StreamLoopAsync(CancellationToken ct)
    {
        int delay = 1000;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var response = await _stream.GetAsync($"pairs/{_pairId}/inbox.json",
                    HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string? eventName = null;
                var data = new StringBuilder();
                while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
                {
                    if (line.Length == 0)
                    {
                        if (eventName is "cancel" or "auth_revoked")
                            throw new HttpRequestException("Inbox stream permission was revoked.");
                        if (data.Length > 0) { HandleEvent(eventName, data.ToString()); delay = 1000; }
                        eventName = null; data.Clear();
                    }
                    else if (line.StartsWith("event:", StringComparison.Ordinal)) eventName = line[6..].Trim();
                    else if (line.StartsWith("data:", StringComparison.Ordinal))
                    {
                        if (data.Length > 0) data.Append('\n');
                        data.Append(line[5..].TrimStart());
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch { }
            try { await Task.Delay(delay, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            delay = Math.Min(delay * 2, 30_000);
        }
    }

    private void HandleEvent(string? name, string json)
    {
        if (name is not ("put" or "patch")) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string path = root.GetProperty("path").GetString() ?? "/";
            var data = root.GetProperty("data");
            if (data.ValueKind != JsonValueKind.Object) return;
            if (path == "/")
                foreach (var prop in data.EnumerateObject()) Raise(prop.Name, prop.Value);
            else if (path.Trim('/').IndexOf('/') < 0) Raise(path.Trim('/'), data);
        }
        catch (JsonException) { }
        catch (KeyNotFoundException) { }
    }

    private void Raise(string id, JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object || id.Length > 120 || id.IndexOfAny(['/', '.', '#', '$', '[', ']']) >= 0) return;
        static string Str(JsonElement e, string k, int max)
            => e.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.String
                ? (p.GetString() ?? "")[..Math.Min((p.GetString() ?? "").Length, max)] : "";
        if (!_seen.TryRemember(id)) return;
        var ping = new PingEvent
        {
            Id = id, Type = Str(data, "type", 24), Text = Str(data, "text", 80),
            From = Str(data, "from", 24), Variant = Str(data, "variant", 48), Character = Str(data, "character", 8),
            VariantLocked = data.TryGetProperty("variantLocked", out var locked) && locked.ValueKind is JsonValueKind.True or JsonValueKind.False ? locked.GetBoolean() : null,
            Ts = data.TryGetProperty("ts", out var ts) && ts.ValueKind == JsonValueKind.Number && ts.TryGetInt64(out var ms) ? ms : 0,
        };
        PingReceived?.Invoke(ping);
    }

    public void Defer(string id)
    {
        _seen.Forget(id);
        Interlocked.Exchange(ref _deferred, 1);
    }

    public async Task RefreshDeferredAsync()
    {
        if (Interlocked.Exchange(ref _deferred, 0) == 0) return;
        try
        {
            using var response = await _rest.GetAsync($"pairs/{_pairId}/inbox.json", _cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(_cts.Token).ConfigureAwait(false));
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var prop in doc.RootElement.EnumerateObject()) Raise(prop.Name, prop.Value);
        }
        catch { Interlocked.Exchange(ref _deferred, 1); }
    }

    public void Complete(PingEvent ping, string status)
    {
        if (!ping.IsLocal) _completed.Writer.TryWrite((ping, status));
    }

    private async Task CompletionLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var item in _completed.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                int delay = 1000;
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var changes = new Dictionary<string, object?>
                        {
                            [$"inbox/{item.Ping.Id}"] = null,
                            ["feedback/delivery"] = new
                            {
                                eventId = item.Ping.Id, type = item.Ping.Type, status = item.Status,
                                ts = new Dictionary<string, string> { [".sv"] = "timestamp" },
                            },
                        };
                        using var content = JsonContent(changes);
                        using var request = new HttpRequestMessage(HttpMethod.Patch, $"pairs/{_pairId}.json") { Content = content };
                        using var response = await _rest.SendAsync(request, ct).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        break;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                    catch
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                        delay = Math.Min(30_000, delay * 2);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task<bool> ReplyAsync(PingEvent ping, string variant = "")
    {
        try
        {
            using var content = JsonContent(new
            {
                id = Guid.NewGuid().ToString("N"), replyTo = ping.Id, type = "hug", variant = variant,
                ts = new Dictionary<string, string> { [".sv"] = "timestamp" },
            });
            using var response = await _rest.PutAsync($"pairs/{_pairId}/feedback/reply.json", content, _cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task PutPresenceAsync(CancellationToken ct = default)
    {
        try
        {
            using var content = JsonContent(new { online = true, ts = new Dictionary<string, string> { [".sv"] = "timestamp" } });
            using var response = await _rest.PutAsync($"pairs/{_pairId}/presence.json", content, ct).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task DeletePresenceAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var response = await _rest.DeleteAsync($"pairs/{_pairId}/presence.json", timeout.Token).ConfigureAwait(false);
        }
        catch { }
    }

    private static StringContent JsonContent(object value) => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Stop(); _stream.Dispose(); _rest.Dispose();
        // CTS may still be observed by the async loops; disposal is unnecessary after cancellation.
    }
}
