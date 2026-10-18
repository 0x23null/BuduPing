using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace BuduPing.Services;

/// <summary>
/// Bộ máy tiếng pop kiểu "thang nốt": 10 file pop01..pop10 nhúng trong exe, nốt càng
/// cao khi combo càng dài. Mỗi nốt có MediaPlayer riêng nên các tiếng CHỒNG nhau được
/// khi spam nhanh — SoundPlayer cũ chỉ phát 1 tiếng tại một thời điểm, bấm nhanh là
/// tiếng bị cắt cụt, không tạo cảm giác spam. WAV được trích ra %AppData%/BuduPing/sounds
/// lúc khởi động (MediaPlayer cần đường dẫn file). Mọi lỗi đều nuốt: mất tiếng chứ không crash.
/// </summary>
public sealed class SoundService
{
    private const int Notes = 10;
    private readonly List<MediaPlayer> _players = new();

    public SoundService()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BuduPing", "sounds");
            Directory.CreateDirectory(dir);

            var asm = Assembly.GetExecutingAssembly();
            for (int i = 1; i <= Notes; i++)
            {
                string name = $"pop{i:00}.wav";
                string path = Path.Combine(dir, name);
                if (!File.Exists(path))
                {
                    using var s = asm.GetManifestResourceStream($"BuduPing.Assets.Sounds.{name}");
                    if (s == null) continue;
                    using var fs = File.Create(path);
                    s.CopyTo(fs);
                }
                var p = new MediaPlayer { Volume = 0.95 };
                p.Open(new Uri(path));
                _players.Add(p);
            }
        }
        catch { /* không có tiếng cũng không sao */ }
    }

    /// <summary>Phát nốt theo độ dài combo: tap 1 = nốt thấp nhất, càng spam càng cao (kẹp ở nốt 10).</summary>
    public void Play(int comboCount)
    {
        if (_players.Count == 0) return;
        try
        {
            int idx = Math.Clamp(comboCount - 1, 0, _players.Count - 1);
            var p = _players[idx];
            p.Stop();
            p.Play();
        }
        catch { }
    }
}
