using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Win32;

namespace BuduPing.Core;

/// <summary>Cấu hình app, lưu tại %AppData%/BuduPing/config.json.</summary>
public sealed class AppConfig
{
    public string PairCode { get; set; } = "";
    public bool SoundOn { get; set; } = true;
    public bool Autostart { get; set; } = false;
    public bool HasSeenSetup { get; set; }
    public bool DiscreetMode { get; set; }
    public bool HideMessageText { get; set; }
    public bool ReducedMotion { get; set; }
    public string MonitorDevice { get; set; } = "";
    public string Corner { get; set; } = "BottomRight";
    public double OverlayScale { get; set; } = 1.0;
}

public static class AppConfigStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BuduPing");
    private static readonly string Path_ = Path.Combine(Dir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>Đọc config; lần đầu chưa có thì tự gen mã cặp mới và lưu.</summary>
    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(Path_))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Path_));
                if (cfg != null && PairCode.IsValid(cfg.PairCode))
                {
                    cfg.OverlayScale = double.IsFinite(cfg.OverlayScale) ? Math.Clamp(cfg.OverlayScale, 0.75, 1.25) : 1;
                    return cfg;
                }
            }
        }
        catch { /* file hỏng → gen lại */ }

        var fresh = new AppConfig { PairCode = PairCode.Generate(), SoundOn = true, Autostart = false };
        Save(fresh);
        return fresh;
    }

    public static void Save(AppConfig cfg)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path_, JsonSerializer.Serialize(cfg, JsonOpts));
        }
        catch { /* không crash app vì lỗi ghi config */ }
    }

    private static string ExePath()
        => Environment.ProcessPath
           ?? Assembly.GetExecutingAssembly().Location
           ?? throw new InvalidOperationException("Không xác định được đường dẫn exe.");

    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Ghi/xoá HKCU\...\Run theo cfg.Autostart.</summary>
    public static void ApplyAutostart(AppConfig cfg)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;
            if (cfg.Autostart)
                key.SetValue("BuduPing", $"\"{ExePath()}\"");
            else if (key.GetValue("BuduPing") != null)
                key.DeleteValue("BuduPing");
        }
        catch { /* thiếu quyền registry → bỏ qua */ }
    }
}
