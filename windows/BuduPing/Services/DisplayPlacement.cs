using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BuduPing.Services;

public static class DisplayPlacement
{
    public sealed record Display(string Device, string Label, int Left, int Top, int Right, int Bottom, uint Dpi);
    public static List<Display> GetDisplays()
    {
        var list = new List<Display>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref Rect rect, IntPtr data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>(), Device = "" };
            if (GetMonitorInfo(monitor, ref info))
            {
                uint dpi = 96;
                try { if (GetDpiForMonitor(monitor, 0, out var x, out _) == 0) dpi = x; } catch { }
                list.Add(new Display(info.Device, $"Màn hình {list.Count + 1}" + ((info.Flags & 1) != 0 ? " (chính)" : ""),
                    info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom, dpi));
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }

    public static void Place(Window window, string device, string corner, double scale)
    {
        var displays = GetDisplays();
        var d = displays.FirstOrDefault(x => x.Device == device) ?? displays.FirstOrDefault();
        if (d == null) return;
        double factor = d.Dpi / 96.0;
        int w = (int)Math.Round(380 * scale * factor), h = (int)Math.Round(580 * scale * factor);
        int gap = (int)(16 * factor);
        int x = corner.EndsWith("Left", StringComparison.Ordinal) ? d.Left + gap : d.Right - w - gap;
        int y = corner.StartsWith("Top", StringComparison.Ordinal) ? d.Top + gap : d.Bottom - h - gap;
        SetWindowPos(new WindowInteropHelper(window).Handle, IntPtr.Zero, Math.Max(d.Left, x), Math.Max(d.Top, y),
            Math.Min(w, d.Right - d.Left), Math.Min(h, d.Bottom - d.Top), 0x0010 | 0x0004);
    }

    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct MonitorInfo
    {
        public int Size; public Rect Monitor, Work; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
    }
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr dc, ref Rect rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorCallback callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int kind, out uint x, out uint y);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
}
