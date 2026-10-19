using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;

namespace BuduPing.Services;

/// <summary>Opt-in local measurements, plus an idle trim the product owner explicitly asked for.</summary>
public static class MemoryTrim
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    /// <summary>
    /// Idle trim (owner request, 2026-10-19, after measuring ~200MB on v0.2): once the
    /// overlay has been hidden and decoded stickers are released, ask the GC for an
    /// optimized gen-2 collect and hand unreferenced working-set pages back to Windows.
    /// Honest scope: this shrinks the Task Manager number and real committed pages the
    /// process no longer touches; the big structural win is not loading Media
    /// Foundation at all (see SoundService). Never called while a sticker is showing.
    /// </summary>
    public static void TrimIdle(long decodedBytes)
    {
        Record("idle-trim", decodedBytes);
        try { GC.Collect(2, GCCollectionMode.Optimized, blocking: false); } catch { }
        try { EmptyWorkingSet(Process.GetCurrentProcess().Handle); } catch { }
    }

    public static void Record(string phase, long decodedBytes, double? latencyMs = null)
    {
        if (Environment.GetEnvironmentVariable("BUDUPING_DIAGNOSTICS") != "1") return;
        try
        {
            using var process = Process.GetCurrentProcess();
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BuduPing");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "performance.jsonl");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000)
                File.Move(path, path + ".previous", true);
            File.AppendAllText(path, JsonSerializer.Serialize(new
            {
                at = DateTimeOffset.UtcNow, phase, latencyMs, decodedBytes,
                workingSet = process.WorkingSet64, privateBytes = process.PrivateMemorySize64,
                managedBytes = GC.GetTotalMemory(false),
            }) + Environment.NewLine);
        }
        catch { /* Diagnostics must never interrupt delivery. */ }
    }
}
