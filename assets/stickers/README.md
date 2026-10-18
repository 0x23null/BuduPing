# BuduPing Sticker Assets

24 sticker GIFs (nền trong suốt thật) cho 12 loại cảm xúc của BuduPing.
Mỗi type có 2 biến thể, đặt tên `<type>-01.gif`, `<type>-02.gif`.

- **Kích thước:** cạnh dài nhất 240–320px (giữ tỉ lệ gốc, không upscale)
- **Dung lượng:** mỗi file < 600KB (tổng ~6.2MB)
- **Nền:** trong suốt thật (alpha=0) — đã kiểm chứng bằng PIL: alpha ở 4 góc
  của **mọi frame** = 0 trên cả 24 file (24/24 đạt)
- **Xử lý:** `tools/make_transparent.py` — phát hiện màu nền từ 4 viền,
  flood-fill từ 4 góc (bảo vệ nhân vật bị cắt ở cạnh frame), tách frame
  đúng chuẩn GIF (disposal/partial frames), resize, giảm frame giữ nguyên
  tổng thời lượng, mã hoá lại GIF với transparency 1-bit (palette index 255)

## Vị trí file

| Nơi | Đường dẫn |
|---|---|
| Gốc (chuẩn) | `assets/stickers/<type>/<type>-01.gif`, `<type>-02.gif` |
| App Windows (WPF) | `windows/BuduPing/Assets/Stickers/<type>/...` |
| App Android | `android/app/src/main/assets/stickers/<type>/...` |

Cả 3 vị trí chứa file **giống hệt nhau** (copy). App map `type` → tên thư mục.

## Danh sách sticker

| Type | Biến thể | Kích thước | Mô tả | Nguồn |
|---|---|---|---|---|
| `miss` | 2 | 285×250, 180×320 | Yier đợi + Dudu ôm điện thoại "BUBU BUBU BUBU I WANT MY BUBU" | GIFDB (2) |
| `love` | 2 | 250×250, 250×250 | Ôm "I love youuuuuu" + Dudu "i love you" | GIFDB (2) |
| `hug` | 2 | 250×250, 320×320 | Ôm nhau + "peluk" (hug) | GIFDB, Tenor |
| `kiss` | 2 | 320×320, 320×320 | Bubu kiss Dudu + hôn má (cheek kiss) | Tenor (2) |
| `tease` | 2 | 304×250, 279×250 | Chọc Dudu đang đọc sách + véo má Dudu đang ngủ | GIFDB (2) |
| `angry` | 2 | 250×312, 320×320 | "ANGRY BUBU" + Dudu giận (vạch giận) | GIFDB, Tenor |
| `sad` | 2 | 320×320, 302×314 | Bubu khóc trên gối + Yier khóc cầm khăn giấy | Tenor (2) |
| `sorry` | 2 | 320×320, 320×317 | Yier cúi đầu xin lỗi + Yier "打扰了!" (làm phiền rồi) | Tenor, KLIPY |
| `funny` | 2 | 239×320, 320×320 | Yier nhảy múa + Dudu khổng lồ ôm Yier tí hon | GIFDB, Tenor (tkthao219) |
| `cheer` | 2 | 250×250, 250×250 | Bubu lắc Dudu ăn mừng + xoay vòng nhảy | GIFDB (2) |
| `hungry` | 2 | 320×265, 250×238 | Ăn mì/uống trà sữa + Yier ăn kem | Tenor, GIFDB |
| `goodnight` | 2 | 320×320, 250×253 | "good night" + Bubu cõng Yier đang ngủ ("z z") | Tenor, GIFDB |

**Nguồn chi tiết** (URL gốc đã tải):
- Tenor: `media1.tenor.com` / `media.tenor.com` — các sticker `bubu-kisses-dudu`,
  `cheek-kiss-bubu`, `dudu-dudu-angry`, `pillow-bubu-dudu`, `bubu-dudu-twitter`,
  `bubu-onetwo`, `tkthao219-bubududu` (của artist tkthao219),
  `bubu-dudu-eat`, `bubu-dudu-good-night`
- GIFDB: `gifdb.com/images/thumbnail/...` — các GIF `bubu-dudu-waiting`,
  `i-want-my-bubu`, `bubu-dudu-i-love-you-hug`, `bubu-dudu-says-i-love-you`,
  `bubu-dudu-hugs`, `bubu-disturb-dudu-reading`, `bubu-pinch-sleeping-dudu`,
  `angry-bubu-sad-dudu`, `bubu-dancing-alone-dudu`, `bubu-shakes-dudu`,
  `bubu-dudu-round-swinging`, `bubu-eats-ice-cream-dudu`,
  `bubu-sleeping-dudu-carry`
- KLIPY: `static.klipy.com` — sticker tkthao219 (file gốc đã trong suốt sẵn)

## Ghi chú

- Tất cả sticker là fan-art Bubu & Dudu (Yier & Bubu / 一二布布) từ Tenor/GIFDB/KLIPY,
  dùng cho mục đích cá nhân trong app BuduPing.
- `hug/hug-01.gif`: nhân vật trắng có sọc vằn (phong cách "mặc costume hổ" của fan-art).
- Một số sticker có chữ gốc (EN/CN) được giữ nguyên như bản gốc.
- Muốn thêm/thay sticker: chạy
  `python3 tools/make_transparent.py input.gif assets/stickers/<type>/<type>-03.gif`
  rồi copy sang 2 thư mục app.
