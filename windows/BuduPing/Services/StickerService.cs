using System.IO;
using System.Windows.Media.Imaging;

namespace BuduPing.Services;

/// <summary>Loại hiệu ứng hạt theo cảm xúc (chỉ quanh sticker, TUYỆT ĐỐI không toàn màn hình).</summary>
public enum ParticleKind
{
    HeartRise,   // love, kiss: tim bay lên + glow
    HeartFall,   // miss: tim nhỏ rơi chậm
    Ring,        // hug: vòng sáng ấm lan 1 nhịp
    Puff,        // angry, tease, jealous: 💢 + rung
    Rain,        // sad: mưa nhỏ
    Bubbles,     // sorry: bong bóng xà phòng
    Star,        // funny: sao nảy
    Confetti,    // cheer: pháo giấy cục bộ
    Zzz,         // goodnight: chữ Z bay lên
    Food,        // hungry: 🍜 bay lên
}

/// <summary>
/// Quản lý sticker GIF: preload + decode sẵn lúc khởi động, chọn ngẫu nhiên chống trùng
/// 2 biến thể vừa hiện. Cấu trúc: Assets/Stickers/&lt;type&gt;/*.gif (thả file vào là dùng).
/// </summary>
public sealed class StickerService
{
    /// <summary>Nhãn tiếng Việt hiện trong bong bóng: "💌 &lt;nhãn&gt; · từ &lt;tên&gt;".</summary>
    public static readonly IReadOnlyDictionary<string, string> TypeLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["love"] = "Yêu ❤️",
            ["miss"] = "Nhớ 🥺",
            ["hug"] = "Ôm 🤗",
            ["kiss"] = "Hôn 😘",
            ["tease"] = "Ghét yêu 😤",
            ["angry"] = "Giận 😠",
            ["sad"] = "Buồn 🥲",
            ["cheer"] = "Cố lên 💪",
            ["goodnight"] = "Ngủ ngon 🌙",
            ["sorry"] = "Xin lỗi 🥹",
            ["funny"] = "Cười 😂",
            ["hungry"] = "Đói 🍜",
            ["jealous"] = "Ghen 😒",
        };

    public static string LabelFor(string type)
        => TypeLabels.TryGetValue(type ?? "", out var l) ? l : (type ?? "love");

    public static ParticleKind ParticleKindFor(string type) => type?.ToLowerInvariant() switch
    {
        "love" or "kiss" => ParticleKind.HeartRise,
        "miss" => ParticleKind.HeartFall,
        "hug" => ParticleKind.Ring,
        "angry" or "tease" or "jealous" => ParticleKind.Puff,
        "sad" => ParticleKind.Rain,
        "sorry" => ParticleKind.Bubbles,
        "funny" => ParticleKind.Star,
        "cheer" => ParticleKind.Confetti,
        "goodnight" => ParticleKind.Zzz,
        "hungry" => ParticleKind.Food,
        _ => ParticleKind.HeartRise,
    };

    private readonly string _root;
    private readonly Random _rng = new();
    private readonly Dictionary<string, List<GifAsset>> _byType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<int>> _recent = new(StringComparer.OrdinalIgnoreCase);

    private sealed class GifAsset
    {
        public byte[] Bytes = Array.Empty<byte>();
        public MemoryStream Stream = null!; // giữ sống để WpfAnimatedGif decode frame
        public BitmapImage Image = null!;
    }

    public StickerService(string stickersRoot) => _root = stickersRoot;

    public IReadOnlyCollection<string> KnownTypes => _byType.Keys;

    /// <summary>Quét thư mục, đọc toàn bộ GIF vào RAM + decode sẵn. Gọi 1 lần lúc khởi động.</summary>
    public void Preload()
    {
        _byType.Clear();
        if (!Directory.Exists(_root)) return;

        foreach (var dir in Directory.GetDirectories(_root))
        {
            string type = Path.GetFileName(dir);
            var assets = new List<GifAsset>();
            foreach (var file in Directory.GetFiles(dir, "*.gif").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(file);
                    var stream = new MemoryStream(bytes, writable: false);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = stream;
                    bmp.EndInit();
                    bmp.Freeze();
                    assets.Add(new GifAsset { Bytes = bytes, Stream = stream, Image = bmp });
                }
                catch { /* file hỏng → bỏ qua, không crash */ }
            }
            if (assets.Count > 0) _byType[type] = assets;
        }
    }

    /// <summary>Chọn ngẫu nhiên 1 biến thể của type, tránh lặp 2 biến thể vừa hiện gần nhất.</summary>
    public BitmapImage? GetRandom(string type)
    {
        if (!_byType.TryGetValue(type ?? "", out var list) || list.Count == 0)
        {
            // fallback: type lạ → lấy bừa 1 gif bất kỳ còn hơn hiện trống
            var any = _byType.Values.FirstOrDefault(l => l.Count > 0);
            return any?[_rng.Next(any.Count)].Image;
        }

        if (!_recent.TryGetValue(type!, out var recent))
        {
            recent = new Queue<int>(2);
            _recent[type!] = recent;
        }

        int idx;
        if (list.Count <= 2)
        {
            // ít biến thể: chỉ tránh lặp đúng cái vừa hiện
            int last = recent.Count > 0 ? recent.Last() : -1;
            do { idx = _rng.Next(list.Count); } while (list.Count > 1 && idx == last);
        }
        else
        {
            var banned = new HashSet<int>(recent);
            do { idx = _rng.Next(list.Count); } while (banned.Contains(idx));
        }

        recent.Enqueue(idx);
        while (recent.Count > 2) recent.Dequeue();
        return list[idx].Image;
    }
}
