using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using BuduPing.Core;
using BuduPing.Services;
using BuduPing.Ui;

namespace BuduPing;

public partial class App
{
    private sealed record MemorySample(string Phase, long AtMs, long PrivateBytes, long WorkingSet, long ManagedBytes);

    // This same harness is compiled into the v0.2.1 baseline by the CI preparation script.
    // It never loads personal config, pairs, starts Firebase, or forces a collection/working-set trim.
    private void StartMemoryReview(string directory)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var start = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1) };
        start.Tick += async (_, _) => { start.Stop(); await RunAsync(); };
        start.Start();

        async Task RunAsync()
        {
            Directory.CreateDirectory(directory);
            int status = 0;
            var samples = new List<MemorySample>();
            var elapsed = Stopwatch.StartNew();
            string phase = "startup";
            using var process = Process.GetCurrentProcess();
            void Sample()
            {
                process.Refresh();
                samples.Add(new(phase, elapsed.ElapsedMilliseconds, process.PrivateMemorySize64, process.WorkingSet64, GC.GetTotalMemory(false)));
            }
            var sampler = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            sampler.Tick += (_, _) => Sample();
            try
            {
                Config = new AppConfig { PairCode = PairCode.Generate(), HasSeenSetup = true, SoundOn = true };
                _stickers = new StickerService(Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers"));
                _stickers.Preload();
                _sounds = new SoundService();
                _overlay = new OverlayWindow(_stickers, this);
                Sample(); sampler.Start(); await Task.Delay(1500);
                phase = "moods";
                var scenes = new[] { ("kiss", "kiss-01"), ("miss", "miss-04"), ("sorry", "sorry-02"),
                    ("love", "love-03"), ("hug", "hug-04"), ("sad", "sad-01") };
                for (int round = 0; round < 2; round++)
                    foreach (var (type, variant) in scenes)
                    {
                        _overlay.ShowPing(new PingEvent { Id = Guid.NewGuid().ToString("N"), Type = type, Variant = variant, IsLocal = true });
                        await Task.Delay(3200); Sample();
                    }
                // Let the final standalone mood leave before the same-emotion spam.
                await Task.Delay(3200);
                phase = "combo";
                for (int i = 0; i < 20; i++)
                {
                    _overlay.ShowPing(new PingEvent { Type = "love", IsLocal = true });
                    await Task.Delay(75);
                }
                await Task.Delay(3500);
                // Stop the 500 ms process-query sampler before timing idle CPU.
                // Otherwise the profiler's own queries are counted as app idle work.
                phase = "idle"; Sample(); sampler.Stop(); process.Refresh();
                double idleCpuStart = process.TotalProcessorTime.TotalMilliseconds;
                var idle = Stopwatch.StartNew();
                await Task.Delay(25000); Sample(); process.Refresh();
                double idleCpuMs = process.TotalProcessorTime.TotalMilliseconds - idleCpuStart;
                var result = new
                {
                    kind = "offline-memory-review", runtime = RuntimeInformation.FrameworkDescription,
                    os = RuntimeInformation.OSDescription, at = DateTimeOffset.UtcNow,
                    peakPrivateBytes = samples.Max(s => s.PrivateBytes), peakWorkingSet = samples.Max(s => s.WorkingSet),
                    settledPrivateBytes = samples[^1].PrivateBytes, settledWorkingSet = samples[^1].WorkingSet,
                    idleCpuMs, idleWallMs = idle.Elapsed.TotalMilliseconds,
                    idleOneCorePercent = idleCpuMs / idle.Elapsed.TotalMilliseconds * 100,
                    samples,
                };
                File.WriteAllText(Path.Combine(directory, "memory.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception e) { status = 1; File.WriteAllText(Path.Combine(directory, "error.txt"), e.ToString()); }
            finally
            {
                sampler.Stop(); _overlay?.ResetPair(); _sounds?.Dispose(); _exiting = true; Shutdown(status);
            }
        }
    }
}
