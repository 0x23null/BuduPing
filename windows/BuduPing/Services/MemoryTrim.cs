using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BuduPing.Services;

/// <summary>
/// Nhả RAM khi rảnh: GC dọn + EmptyWorkingSet để Task Manager phản ánh đúng lượng
/// RAM app thực sự cần lúc chạy nền. WPF + .NET runtime có sàn RAM sẵn nên đây là
/// cách kéo con số idle xuống thấp nhất có thể mà không đổi stack. Chống spam bằng
/// throttle 20s; chạy trên background thread, không bao giờ làm đứng UI.
/// </summary>
public static class MemoryTrim
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    private static DateTime _lastUtc = DateTime.MinValue;
    private static readonly object _gate = new();

    public static void Request()
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUtc).TotalSeconds < 20) return;
            _lastUtc = now;
        }
        _ = Task.Run(() =>
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false);
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }
            catch { /* trim là best-effort, lỗi thì thôi */ }
        });
    }
}
