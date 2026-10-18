using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BuduPing.Services;

/// <summary>
/// Bộ máy tiếng pop "thang nốt": 10 mẫu pop01..pop10 nhúng trong exe, nốt càng cao và
/// tiếng càng rõ khi combo càng dài; 8 khe phát song song nên các tiếng CHỒNG nhau được
/// khi spam nhanh.
///
/// Bản 0.2.1 — viết lại vì RAM: bản cũ dùng MediaPlayer, kéo cả Media Foundation vào
/// tiến trình (vài chục MB) chỉ để phát mấy file WAV 11KB, lại phải trích WAV ra
/// %AppData%. Bản này phát PCM qua waveOut (winmm, API âm thanh cổ điển của Windows):
/// toàn bộ 10 nốt nằm trong RAM dạng mẫu 16-bit (~115KB), không nạp thêm framework
/// media nào, không ghi file ra đĩa. Nếu waveOut mở không được (máy không có thiết bị
/// âm thanh) thì lùi về SoundPlayer cho nốt cao nhất. Mọi lỗi đều nuốt: mất tiếng chứ
/// không crash.
/// </summary>
public sealed class SoundService : IDisposable
{
    private const int Notes = 10;
    private const int Slots = 8;
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
    }

    private readonly List<short[]> _notes = new();
    private int _sampleRate = 22050;
    private readonly List<Slot> _slots = new();
    private readonly WaveOutProc _callback; // giữ sống suốt đời service — native gọi lại qua delegate này
    private readonly System.Media.SoundPlayer? _fallback;
    private bool _disposed;

    public SoundService()
    {
        _callback = OnWaveOut;
        byte[]? loudestWav = null;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            for (int i = 1; i <= Notes; i++)
            {
                using var s = asm.GetManifestResourceStream($"BuduPing.Assets.Sounds.pop{i:00}.wav");
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
                int maxLen = _notes.Max(n => n.Length);
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

    /// <summary>Phát nốt theo độ dài combo: tap 1 = nốt thấp nhất, càng spam càng cao (kẹp ở nốt 10), âm lượng cũng nhích lên.</summary>
    public void Play(int comboCount)
    {
        if (_disposed) return;
        if (_slots.Count == 0)
        {
            try { _fallback?.Play(); } catch { }
            return;
        }
        try
        {
            int idx = Math.Clamp(comboCount - 1, 0, _notes.Count - 1);
            var samples = _notes[idx];
            double gain = Math.Min(0.35 + 0.05 * comboCount, 0.85);
            var slot = _slots.FirstOrDefault(s => !s.Busy) ?? _slots[0];
            if (slot.Busy) waveOutReset(slot.Handle); // cướp khe bận nhất: cắt tiếng cũ của khe này
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
            uint size = (uint)Marshal.SizeOf<WaveHeader>();
            waveOutUnprepareHeader(slot.Handle, slot.HeaderPtr, size);
            if (waveOutPrepareHeader(slot.Handle, slot.HeaderPtr, size) != 0) return;
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

    public void Dispose()
    {
        _disposed = true;
        foreach (var slot in _slots)
        {
            try { waveOutReset(slot.Handle); waveOutClose(slot.Handle); } catch { }
            if (slot.Pin.IsAllocated) slot.Pin.Free();
            if (slot.HeaderPtr != IntPtr.Zero) Marshal.FreeHGlobal(slot.HeaderPtr);
        }
        _slots.Clear();
        try { _fallback?.Dispose(); } catch { }
    }
}
