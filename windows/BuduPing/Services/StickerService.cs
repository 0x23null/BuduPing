using System.IO;
using System.Text.Json;
using System.Windows;
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
            ["hello"] = "Chào",
            ["love"] = "Yêu",
            ["miss"] = "Nhớ",
            ["hug"] = "Ôm",
            ["kiss"] = "Hôn",
            ["tease"] = "Ghét yêu",
            ["angry"] = "Giận",
            ["sad"] = "Buồn",
            ["cheer"] = "Cố lên",
            ["goodnight"] = "Ngủ ngon",
            ["sorry"] = "Xin lỗi",
            ["funny"] = "Cười",
            ["hungry"] = "Đói",
            ["jealous"] = "Ghen 😒",
        };

    public static string LabelFor(string type)
        => TypeLabels.TryGetValue(type ?? "", out var l) ? l : (type ?? "love");

    public static ParticleKind ParticleKindFor(string type) => type?.ToLowerInvariant() switch
    {
        "love" or "kiss" => ParticleKind.HeartRise,
        "miss" => ParticleKind.HeartFall,
        "hello" => ParticleKind.Star,
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
        public string Key = "";
        public string Variant = "";              // định danh duy nhất (tên resource / đường dẫn)
        public byte[] Bytes = Array.Empty<byte>(); // GIF nén — giữ trong RAM rất nhẹ
    }

    // Cache decode lười: stream phải giữ sống để WpfAnimatedGif đọc frame.
    private sealed class DecodedGif
    {
        public MemoryStream Stream = null!;
        public BitmapImage Image = null!;
        public long Cost;
    }

    private const int MaxDecoded = 4;
    private const long MaxDecodedBytes = 16L * 1024 * 1024;
    private readonly Dictionary<string, StickerInfo> _catalog = new();
    public StickerInfo? LastInfo { get; private set; }
    public long DecodedBytes => _decoded.Values.Sum(x => x.Cost);

    public sealed class StickerInfo
    {
        public string id { get; set; } = "";
        public int width { get; set; }
        public int height { get; set; }
        public int[] bounds { get; set; } = Array.Empty<int>();
        public long estimatedBytes { get; set; }
        public List<string> characters { get; set; } = new(); // "bubu" = white panda, "dudu" = brown bear
    }
    private readonly Dictionary<string, DecodedGif> _decoded = new(StringComparer.Ordinal);
    private readonly Queue<string> _decodedOrder = new();
    private readonly Dictionary<string, int> _pins = new(StringComparer.Ordinal);
    private string _lastShownKey = ""; // gif đang hiện trên overlay — không evict stream của nó

    public StickerService(string stickersRoot) => _root = stickersRoot;

    public IReadOnlyCollection<string> KnownTypes => _byType.Keys;

    /// <summary>Nạp danh mục sticker (chỉ bytes, KHÔNG decode). Gọi 1 lần lúc khởi động.</summary>
    public void Preload()
    {
        _byType.Clear();
        _catalog.Clear();
        try
        {
            using var catalog = typeof(StickerService).Assembly.GetManifestResourceStream("BuduPing.Assets.Stickers.catalog.json");
            if (catalog != null)
                foreach (var info in JsonSerializer.Deserialize<List<StickerInfo>>(catalog) ?? new())
                    _catalog[info.id] = info;
        }
        catch { }

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
        string variant = key.StartsWith("file:", StringComparison.Ordinal)
            ? Path.GetFileNameWithoutExtension(key[5..]) : key.Split('.')[^2];
        list.Add(new GifSource { Key = key, Variant = variant, Bytes = bytes });
    }

    /// <summary>Nhả toàn bộ GIF đã decode (gọi sau khi overlay ẩn hẳn để trả RAM về idle).</summary>
    public void ReleaseDecoded()
    {
        foreach (var d in _decoded.Values)
        {
            try { d.Stream.Dispose(); } catch { }
        }
        _decoded.Clear();
        _decodedOrder.Clear();
        _pins.Clear();
        _lastShownKey = "";
        LastInfo = null;
    }

    /// <summary>Decode 1 GIF khi cần (cache tối đa MaxDecoded cái gần nhất).</summary>
    private BitmapImage? Decode(GifSource src)
    {
        LastInfo = _catalog.GetValueOrDefault(src.Variant);
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
            long cost = LastInfo?.estimatedBytes ?? (long)bmp.PixelWidth * bmp.PixelHeight * 4 * 30;
            _decoded[src.Key] = new DecodedGif { Stream = stream, Image = bmp, Cost = cost };
            _decodedOrder.Enqueue(src.Key);
            _lastShownKey = src.Key;
            TrimDecoded();
            return bmp;
        }
        catch { return null; }
    }

    public void Pin(BitmapImage image)
    {
        var entry = _decoded.FirstOrDefault(x => ReferenceEquals(x.Value.Image, image));
        if (entry.Key != null) _pins[entry.Key] = _pins.GetValueOrDefault(entry.Key) + 1;
    }

    public void Unpin(BitmapImage image)
    {
        var entry = _decoded.FirstOrDefault(x => ReferenceEquals(x.Value.Image, image));
        if (entry.Key != null && _pins.TryGetValue(entry.Key, out int count))
        {
            if (count <= 1) _pins.Remove(entry.Key); else _pins[entry.Key] = count - 1;
        }
        TrimDecoded();
    }

    private void TrimDecoded()
    {
        int attempts = _decodedOrder.Count;
        while (_decoded.Count > 1 && (_decoded.Count > MaxDecoded || DecodedBytes > MaxDecodedBytes) && attempts-- > 0)
        {
            string oldest = _decodedOrder.Dequeue();
            if (oldest == _lastShownKey || _pins.ContainsKey(oldest)) { _decodedOrder.Enqueue(oldest); continue; }
            if (_decoded.Remove(oldest, out var evicted)) evicted.Stream.Dispose();
        }
    }

    /// <summary>Chọn ngẫu nhiên 1 biến thể của type, tránh lặp 2 biến thể vừa hiện gần nhất.</summary>
    /// <summary>Nhân vật xuất hiện trong 1 biến thể (theo catalog): ["bubu"], ["dudu"] hoặc cả hai.</summary>
    private List<string> CharactersOf(string variant)
        => _catalog.TryGetValue(variant, out var info) ? info.characters : new List<string>();

    /// <summary>
    /// Chọn tên biến thể theo nhân vật của người gửi: ưu tiên sticker CHỈ có nhân vật của mình,
    /// rồi tới sticker có cả hai; không bao giờ chọn sticker solo của nhân vật còn lại.
    /// </summary>
    public bool HasForCharacter(string type, string character)
        => _byType.TryGetValue(type, out var list) && list.Any(x =>
            string.IsNullOrEmpty(character) || CharactersOf(x.Variant).Contains(character));

    public string PickVariant(string type, string myCharacter = "")
    {
        if (!_byType.TryGetValue(type ?? "", out var list) || list.Count == 0) return "";
        if (string.IsNullOrEmpty(myCharacter)) return list[_rng.Next(list.Count)].Variant;
        var solo = list.Where(x => CharactersOf(x.Variant) is { Count: 1 } c && c[0] == myCharacter).ToList();
        var pool = solo.Count > 0 ? solo : list.Where(x => CharactersOf(x.Variant).Count >= 2).ToList();
        return pool.Count > 0 ? pool[_rng.Next(pool.Count)].Variant : "";
    }

    /// <summary>Như GetRandom nhưng biến thể được lọc theo nhân vật của người gửi.</summary>
    public BitmapImage? GetRandomForCharacter(string type, string myCharacter)
    {
        var variant = PickVariant(type, myCharacter);
        return string.IsNullOrEmpty(variant) ? null : GetRandom(type, variant, myCharacter);
    }

    public BitmapImage? GetRandom(string type, string variant = "", string character = "")
    {
        if (!_byType.TryGetValue(type ?? "", out var sources)) return null;
        var list = sources.Where(x => string.IsNullOrEmpty(character) || CharactersOf(x.Variant).Contains(character)).ToList();
        if (list.Count == 0) return null;

        if (!string.IsNullOrEmpty(variant))
        {
            var chosen = list.FirstOrDefault(x => string.Equals(x.Variant, variant, StringComparison.Ordinal));
            return chosen == null ? null : Decode(chosen);
        }
        if (!_recent.TryGetValue(type + ":" + character, out var recent))
        {
            recent = new Queue<int>(2);
            _recent[type + ":" + character] = recent;
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
            var eligible = Enumerable.Range(0, list.Count).Where(i => !banned.Contains(i)).ToArray();
            idx = eligible.Length == 0 ? _rng.Next(list.Count) : eligible[_rng.Next(eligible.Length)];
        }

        recent.Enqueue(idx);
        while (recent.Count > 2) recent.Dequeue();
        return Decode(list[idx]);
    }
}
