using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BuduPing.Services;

/// <summary>Short atata PCM cues via the existing winmm engine: two voices, no playback queue,
/// no Media Foundation, no media files extracted at runtime. Cues are prepared offline.</summary>
public sealed class SoundService : IDisposable
{
    private static readonly string[] CueNames = { "atata", "ta", "ta-accent" };

    /// <summary>Per-character voice cues (measured from the source clips — docs/SOUND-REPORT.md):
    /// the white panda's voice sits ~an octave above the brown bear's. Each set is
    /// [greeting, tap, accent], the same slots Play() already uses for the default set.</summary>
    private static readonly Dictionary<string, string[]> VoiceCues = new()
    {
        ["bubu"] = new[] { "bubu-atata", "bubu-ta", "bubu-lala" },
        ["dudu"] = new[] { "dudu-tata", "dudu-hum", "dudu-tata" },
    };
    private const int Slots = 2;
    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint CallbackFunction = 0x00030000;
    private const uint WomDone = 0x3BD;
    private const ushort WaveFormatPcm = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    private delegate void WaveOutProc(IntPtr hwo, uint uMsg, IntPtr instance, IntPtr param1, IntPtr param2);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutOpen(out IntPtr hWaveOut, uint deviceID, ref WaveFormatEx format, WaveOutProc callback, IntPtr instance, uint flags);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint cbWaveOutHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint cbWaveOutHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint cbWaveOutHdr);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutReset(IntPtr hWaveOut);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint waveOutClose(IntPtr hWaveOut);

    private sealed class Slot
    {
        public IntPtr Handle;
        public IntPtr HeaderPtr;
        public GCHandle Pin;
        public short[] Buffer = Array.Empty<short>();
        public volatile bool Busy;
        public int Generation;
        public bool Prepared;
    }

    private readonly List<short[]> _notes = new();
    private readonly Dictionary<string, List<short[]>> _voices = new();
    private int _sampleRate = 22050;
    private readonly List<Slot> _slots = new();
    private readonly WaveOutProc _callback; // giữ sống suốt đời service — native gọi lại qua delegate này
    private readonly System.Media.SoundPlayer? _fallback;
    private bool _disposed;
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private long _lastBeat = -Core.PresentationPolicy.AudioGapMs;

    public SoundService()
    {
        _callback = OnWaveOut;
        byte[]? loudestWav = null;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (string cue in CueNames)
            {
                using var s = asm.GetManifestResourceStream($"BuduPing.Assets.Sounds.{cue}.wav");
                if (s == null) continue;
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                var (samples, rate) = ParseWav(bytes);
                if (samples.Length == 0) continue;
                _notes.Add(samples);
                _sampleRate = rate;
                loudestWav = bytes;
            }
            foreach (var (character, cues) in VoiceCues)
            {
                var set = new List<short[]>();
                foreach (string cue in cues)
                {
                    using var s = asm.GetManifestResourceStream($"BuduPing.Assets.Sounds.{cue}.wav");
                    if (s == null) continue;
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    var (samples, rate) = ParseWav(ms.ToArray());
                    if (samples.Length == 0) continue;
                    set.Add(samples);
                    _sampleRate = rate;
                }
                if (set.Count > 0) _voices[character] = set;
            }
        }
        catch { /* không có tiếng cũng không sao */ }

        if (_notes.Count > 0)
        {
            try
            {
                var fmt = new WaveFormatEx
                {
                    wFormatTag = WaveFormatPcm,
                    nChannels = 1,
                    nSamplesPerSec = (uint)_sampleRate,
                    nAvgBytesPerSec = (uint)(_sampleRate * 2),
                    nBlockAlign = 2,
                    wBitsPerSample = 16,
                    cbSize = 0,
                };
                int maxLen = _notes.Concat(_voices.Values.SelectMany(v => v)).Max(n => n.Length);
                for (int i = 0; i < Slots; i++)
                {
                    if (waveOutOpen(out var h, WaveMapper, ref fmt, _callback, IntPtr.Zero, CallbackFunction) != 0) break;
                    var slot = new Slot { Handle = h, Buffer = new short[maxLen] };
                    slot.Pin = GCHandle.Alloc(slot.Buffer, GCHandleType.Pinned);
                    slot.HeaderPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHeader>());
                    _slots.Add(slot);
                }
            }
            catch { }
        }

        if (_slots.Count == 0 && loudestWav != null)
        {
            try
            {
                _fallback = new System.Media.SoundPlayer(new MemoryStream(loudestWav));
                _fallback.Load();
            }
            catch { }
        }
    }

    private void OnWaveOut(IntPtr hwo, uint uMsg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (uMsg != WomDone || param1 == IntPtr.Zero) return;
        try
        {
            var hdr = Marshal.PtrToStructure<WaveHeader>(param1);
            foreach (var slot in _slots)
            {
                // Chỉ nhả khe khi callback thuộc đúng lượt phát hiện tại (khe có thể đã bị "cướp" để phát tiếng mới).
                if (slot.Handle == hwo && hdr.dwUser == (IntPtr)slot.Generation) slot.Busy = false;
            }
        }
        catch { }
    }

    /// <summary>New reaction: the sender's character greets in its own voice.
    /// Following taps: that voice's short beat; accents at combo milestones.
    /// Unknown character = the original generic atata set.</summary>
    public void Play(int comboCount, string character = "", string type = "")
    {
        if (_disposed) return;
        long now = _clock.ElapsedMilliseconds;
        if (now - _lastBeat < Core.PresentationPolicy.AudioGapMs) return;
        _lastBeat = now;
        if (_slots.Count == 0)
        {
            try { _fallback?.Play(); } catch { }
            return;
        }
        try
        {
            var set = character != null && _voices.TryGetValue(character, out var voice) ? voice : _notes;
            int idx = Math.Min(comboCount <= 1 ? 0 : Core.PresentationPolicy.IsAccent(comboCount) ? 2 : 1, set.Count - 1);
            // A somber mood from the brown bear answers with his low hum, not a greeting.
            if (comboCount <= 1 && character == "dudu" && set.Count > 1 &&
                type is "sad" or "sorry" or "sleepy" or "goodnight" or "angry") idx = 1;
            var samples = set[idx];
            double gain = Core.PresentationPolicy.IsAccent(comboCount) ? .42 : .36;
            var slot = _slots.FirstOrDefault(s => !s.Busy);
            if (slot == null) return; // Saturated: coalesce, never steal a live buffer or build a queue.
            uint size = (uint)Marshal.SizeOf<WaveHeader>();
            if (slot.Prepared)
            {
                if (waveOutUnprepareHeader(slot.Handle, slot.HeaderPtr, size) != 0) return;
                slot.Prepared = false;
            }
            for (int i = 0; i < samples.Length; i++)
            {
                int v = (int)(samples[i] * gain);
                slot.Buffer[i] = (short)Math.Clamp(v, short.MinValue, short.MaxValue);
            }
            slot.Generation++;
            var hdr = new WaveHeader
            {
                lpData = slot.Pin.AddrOfPinnedObject(),
                dwBufferLength = (uint)(samples.Length * 2),
                dwBytesRecorded = 0,
                dwUser = (IntPtr)slot.Generation,
                dwFlags = 0,
                dwLoops = 0,
                lpNext = IntPtr.Zero,
                reserved = IntPtr.Zero,
            };
            Marshal.StructureToPtr(hdr, slot.HeaderPtr, false);
            if (waveOutPrepareHeader(slot.Handle, slot.HeaderPtr, size) != 0) return;
            slot.Prepared = true;
            slot.Busy = true;
            if (waveOutWrite(slot.Handle, slot.HeaderPtr, size) != 0) slot.Busy = false;
        }
        catch { }
    }

    /// <summary>Đọc chunk fmt/data của WAV PCM 16-bit; stereo được trộn xuống mono. Trả (rỗng, rate) nếu không đọc được.</summary>
    private static (short[] Samples, int Rate) ParseWav(byte[] wav)
    {
        try
        {
            int channels = 1, rate = 22050, bits = 16;
            int pos = 12; // bỏ qua header RIFF
            while (pos + 8 <= wav.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                if (size < 0 || pos + 8 + size > wav.Length + 1) break;
                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(wav, pos + 10);
                    rate = BitConverter.ToInt32(wav, pos + 12);
                    bits = BitConverter.ToInt16(wav, pos + 22);
                }
                else if (id == "data")
                {
                    if (bits != 16) return (Array.Empty<short>(), rate);
                    int count = Math.Min(size, wav.Length - (pos + 8)) / 2;
                    var all = new short[count];
                    Buffer.BlockCopy(wav, pos + 8, all, 0, count * 2);
                    if (channels == 2)
                    {
                        var mono = new short[count / 2];
                        for (int i = 0; i < mono.Length; i++)
                            mono[i] = (short)((all[2 * i] + all[2 * i + 1]) / 2);
                        return (mono, rate);
                    }
                    return (all, rate);
                }
                pos += 8 + size + (size % 2);
            }
        }
        catch { }
        return (Array.Empty<short>(), 22050);
    }

    public void Stop()
    {
        foreach (var slot in _slots)
            try { waveOutReset(slot.Handle); slot.Busy = false; } catch { }
        try { _fallback?.Stop(); } catch { }
    }

    public void Dispose()
    {
        _disposed = true;
        foreach (var slot in _slots)
        {
            try
            {
                waveOutReset(slot.Handle);
                if (slot.Prepared) waveOutUnprepareHeader(slot.Handle, slot.HeaderPtr, (uint)Marshal.SizeOf<WaveHeader>());
                waveOutClose(slot.Handle);
            }
            catch { }
            if (slot.Pin.IsAllocated) slot.Pin.Free();
            if (slot.HeaderPtr != IntPtr.Zero) Marshal.FreeHGlobal(slot.HeaderPtr);
        }
        _slots.Clear();
        try { _fallback?.Dispose(); } catch { }
    }
}
