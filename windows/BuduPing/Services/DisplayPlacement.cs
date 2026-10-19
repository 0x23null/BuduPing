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
        int w = (int)Math.Round(window.Width * factor), h = (int)Math.Round(window.Height * factor);
        int gap = (int)(16 * factor);
        int x = corner.EndsWith("Left", StringComparison.Ordinal) ? d.Left + gap : d.Right - w - gap;
        // Bottom corners perch ON the work-area edge (the taskbar's top), no
        // vertical gap — the buddy sits on the taskbar like a desktop pet.
        int y = corner.StartsWith("Top", StringComparison.Ordinal) ? d.Top + gap : d.Bottom - h;
        SetWindowPos(new WindowInteropHelper(window).Handle, IntPtr.Zero, Math.Max(d.Left, x), Math.Max(d.Top, y),
            Math.Min(w, d.Right - d.Left), Math.Min(h, d.Bottom - d.Top), 0x0010 | 0x0004);
    }

    /// <summary>
    /// Edge-peek placement: the window straddles the RIGHT rim of the target
    /// display — only the left ~58% stays on screen, so the character appears
    /// cropped by the screen edge itself (desktop-buddy style). Bottom-aligned.
    /// </summary>
    public static void PlacePeekRight(Window window, string device)
    {
        var displays = GetDisplays();
        var d = displays.FirstOrDefault(x => x.Device == device) ?? displays.FirstOrDefault();
        if (d == null) return;
        double factor = d.Dpi / 96.0;
        int w = (int)Math.Round(window.Width * factor), h = (int)Math.Round(window.Height * factor);
        int visible = (int)(w * 0.58);
        int x = d.Right - visible;
        int y = d.Bottom - h;
        SetWindowPos(new WindowInteropHelper(window).Handle, IntPtr.Zero, x, Math.Max(d.Top, y), w, h, 0x0010 | 0x0004);
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
