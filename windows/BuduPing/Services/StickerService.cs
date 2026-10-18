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
/// Quản lý sticker GIF: nguồn là EmbeddedResource trong exe (Assets/Stickers/&lt;type&gt;/*.gif),
/// cộng thêm thư mục rời cạnh exe nếu có (dev). Chỉ giữ BYTES nén trong RAM (~6MB);
/// decode BitmapImage lười khi cần và cache tối đa vài cái gần nhất — bản v0.1.0 decode
/// sẵn cả 24 GIF lúc khởi động nên ngốn hơn 100MB RAM.
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
    private readonly Dictionary<string, List<GifSource>> _byType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<int>> _recent = new(StringComparer.OrdinalIgnoreCase);

    private sealed class GifSource
    {
        public string Key = "";              // định danh duy nhất (tên resource / đường dẫn)
        public byte[] Bytes = Array.Empty<byte>(); // GIF nén — giữ trong RAM rất nhẹ
    }

    // Cache decode lười: stream phải giữ sống để WpfAnimatedGif đọc frame.
    private sealed class DecodedGif
    {
        public MemoryStream Stream = null!;
        public BitmapImage Image = null!;
    }

    private const int MaxDecoded = 6;
    private readonly Dictionary<string, DecodedGif> _decoded = new(StringComparer.Ordinal);
    private readonly Queue<string> _decodedOrder = new();
    private string _lastShownKey = ""; // gif đang hiện trên overlay — không evict stream của nó

    public StickerService(string stickersRoot) => _root = stickersRoot;

    public IReadOnlyCollection<string> KnownTypes => _byType.Keys;

    /// <summary>Nạp danh mục sticker (chỉ bytes, KHÔNG decode). Gọi 1 lần lúc khởi động.</summary>
    public void Preload()
    {
        _byType.Clear();

        // 1) GIF nhúng trong exe: BuduPing.Assets.Stickers.<type>.<file>.gif
        try
        {
            var asm = typeof(StickerService).Assembly;
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = name.Split('.');
                if (parts.Length < 3) continue;
                string type = parts[^3]; // segment ngay trước tên file
                try
                {
                    using var s = asm.GetManifestResourceStream(name);
                    if (s == null) continue;
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    AddSource(type, name, ms.ToArray());
                }
                catch { /* resource hỏng → bỏ qua */ }
            }
        }
        catch { /* không có resource nhúng → còn đường thư mục rời bên dưới */ }

        // 2) Thư mục rời cạnh exe (dev / thả thêm sticker): đọc bytes, decode lười như trên
        if (Directory.Exists(_root))
        {
            foreach (var dir in Directory.GetDirectories(_root))
            {
                string type = Path.GetFileName(dir);
                foreach (var file in Directory.GetFiles(dir, "*.gif").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    try { AddSource(type, "file:" + file, File.ReadAllBytes(file)); }
                    catch { /* file hỏng → bỏ qua */ }
                }
            }
        }
    }

    private void AddSource(string type, string key, byte[] bytes)
    {
        if (bytes.Length == 0) return;
        if (!_byType.TryGetValue(type, out var list))
        {
            list = new List<GifSource>();
            _byType[type] = list;
        }
        list.Add(new GifSource { Key = key, Bytes = bytes });
    }

    /// <summary>Decode 1 GIF khi cần (cache tối đa MaxDecoded cái gần nhất).</summary>
    private BitmapImage? Decode(GifSource src)
    {
        if (_decoded.TryGetValue(src.Key, out var hit))
        {
            _lastShownKey = src.Key;
            return hit.Image;
        }
        try
        {
            var stream = new MemoryStream(src.Bytes, writable: false);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();
            _decoded[src.Key] = new DecodedGif { Stream = stream, Image = bmp };
            _decodedOrder.Enqueue(src.Key);
            _lastShownKey = src.Key;
            while (_decoded.Count > MaxDecoded)
            {
                string oldest = _decodedOrder.Dequeue();
                if (oldest == _lastShownKey) continue; // đang hiện → giữ stream sống
                if (_decoded.Remove(oldest, out var evicted))
                    evicted.Stream.Dispose();
            }
            return bmp;
        }
        catch { return null; }
    }

    /// <summary>Chọn ngẫu nhiên 1 biến thể của type, tránh lặp 2 biến thể vừa hiện gần nhất.</summary>
    public BitmapImage? GetRandom(string type)
    {
        if (!_byType.TryGetValue(type ?? "", out var list) || list.Count == 0)
        {
            // fallback: type lạ → lấy bừa 1 gif bất kỳ còn hơn hiện trống
            var any = _byType.Values.FirstOrDefault(l => l.Count > 0);
            return any == null ? null : Decode(any[_rng.Next(any.Count)]);
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
        return Decode(list[idx]);
    }
}
