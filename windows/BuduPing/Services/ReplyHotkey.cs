using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace BuduPing.Services;

public sealed class ReplyHotkey : IDisposable
{
    private const int Id = 0xB0D0;
    private readonly HwndSource _source = new HwndSource(new HwndSourceParameters("BuduPing hotkey")
        { Width = 0, Height = 0, WindowStyle = 0, ParentWindow = new IntPtr(-3) });
    public event Action? Pressed;
    public bool Registered { get; }

    public ReplyHotkey()
    {
        _source.AddHook(Hook);
        Registered = RegisterHotKey(_source.Handle, Id, 0x4000 | 0x0002 | 0x0001, 0x20);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
    {
        if (msg == 0x0312 && wp.ToInt32() == Id) { Pressed?.Invoke(); handled = true; }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (Registered) UnregisterHotKey(_source.Handle, Id);
        _source.RemoveHook(Hook);
        _source.Dispose();
    }

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
