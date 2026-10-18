using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BuduPing.Services;

/// <summary>Opt-in local measurements; no forced GC or working-set eviction.</summary>
public static class MemoryTrim
{
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
