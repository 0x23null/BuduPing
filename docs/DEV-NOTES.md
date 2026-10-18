# BuduPing — Ghi chú phát triển (đọc file này trước khi sửa code)

Tài liệu bàn giao ngữ cảnh cho bất kỳ ai (người hoặc AI) tiếp tục phát triển BuduPing.
Cập nhật: 2026-10-19. v0.1.2 đã phát hành; v0.1.3 xuất hiện trên main trong lúc phát triển.
Nhánh `codex/buduping-0.2` đang triển khai bản nâng cấp: đọc [DEVELOPMENT-REPORT-0.2.0.md](DEVELOPMENT-REPORT-0.2.0.md)
để biết code đã thay đổi, hợp đồng feedback, kết quả kiểm chứng và phần cần test máy thật.
Không dùng các dòng lịch sử "đang làm" bên dưới để suy ra trạng thái release.

## 1. Tổng quan

- App cặp đôi 2 người dùng: **Android** (Kotlin + Jetpack Compose, `android/`) là phía GỬI,
  **Windows overlay** (C# .NET 8 WPF, `windows/BuduPing/`) là phía NHẬN — sticker Bubu & Dudu
  (Yier & Bubu / 一二布布) nảy ở góc phải dưới màn hình khi đối phương bấm 1 trong 12 cảm xúc.
- Hạ tầng: **Firebase Realtime Database** (Spark free, asia-southeast1). Không đăng nhập,
  không server riêng. Ghép nối bằng mã `XXXX-XXXX` (Crockford Base32 + checksum) —
  thuật toán ở `docs/pair-code.md`, hướng dẫn Firebase ở `docs/firebase-setup.md`,
  rules ở `firebase/database.rules.json`.
- 12 loại cảm xúc (type dùng trong code): `miss, love, hug, kiss, tease, angry, sad,
  sorry, funny, cheer, hungry, goodnight` (còn `jealous` là legacy trong bản đồ nhãn/hạt).

## 2. HỢP ĐỒNG DỮ LIỆU FIREBASE — quan trọng nhất, sai là app "chết lặng"

Path gốc: `/pairs/{pairId}`, `pairId` = mã cặp đã chuẩn hoá, 8 ký tự, **không dấu gạch**
(vd mã hiển thị `2HBR-NKWT` → pairId `2HBRNKWT`).

- **Inbox**: `/pairs/{pairId}/inbox/{pushKey}` — điện thoại `push()` một event:
  `{ "id": "<uuid>", "type": "kiss", "text": "...", "from": "<tên người gửi>", "ts": <epoch ms> }`.
  - ⚠️ Field `id` trong body CHỈ để tham khảo. Khi desktop xoá event sau khi hiện,
    **bắt buộc xoá theo pushKey** (tên node), xoá theo `id` UUID là không trúng node nào.
    (Đây là bug thật ở v0.1.0: tin nhắn tồn đọng mãi không xoá được.)
  - Desktop nghe inbox bằng REST streaming (SSE, `Accept: text/event-stream`), không polling.
- **Presence**: `/pairs/{pairId}/presence` — desktop ghi mỗi ~20s và khi thoát thì xoá:
  `{ "online": true, "ts": <epoch ms> }`.
  - ⚠️ Tên trường là **`ts`**, KHÔNG phải `lastSeen`. Điện thoại tính online =
    `online == true && now - ts < 60_000`. (Bug thật ở v0.1.0: điện thoại đọc `lastSeen`
    → luôn báo máy tính offline dù mã đúng.)
- Mọi thay đổi ở một phía PHẢI kiểm tra phía còn lại đọc đúng tên trường/đường dẫn.
  Hai app từng được viết song song và lệch hợp đồng ở đúng 2 chỗ trên.

## 3. Quy tắc đóng gói Windows — Release chỉ có 1 file exe

- Release GitHub chỉ đính kèm **một file `BuduPing.exe`** (self-contained single-file).
  Hệ quả: **mọi tài nguyên runtime (sticker GIF, file .wav, icon) PHẢI là
  `EmbeddedResource` nhúng trong assembly.** Để dạng `Content` copy cạnh exe là người
  dùng sẽ KHÔNG có file đó. (Bug thật ở v0.1.0: sticker không hiện vì GIF nằm ngoài exe.)
- Truy cập tài nguyên nhúng qua `Assembly.GetManifestResourceStream(...)`, tên dạng
  `BuduPing.Assets.Stickers.<type>.<file>.gif`, `BuduPing.Assets.Sounds.pop.wav`.

## 4. Build / CI / Release (đã tự động hoá hoàn toàn)

- 4 workflow trong `.github/workflows/`: `build-apk.yml`, `build-windows.yml`,
  `ui-review.yml` (emulator Android) và `release.yml` (build cả hai nền tảng).
- Mỗi workflow có bước cuối chạy `.github/scripts/report-ci.sh`: commit trạng thái +
  đuôi log build vào nhánh **`ci-reports`** (`reports/<tên>.md`). Nhờ vậy kết quả build
  đọc được qua git thuần — khi build đỏ, đọc report đúng SHA trước khi đoán lỗi.
- Từ 0.2.0: đổi root `VERSION` trên main sẽ kích hoạt release. Hai build phải cùng
  thành công trước khi job publish tạo tag và đính kèm cả `BuduPing.apk` + `BuduPing.exe`.
  Push tag `v*` và workflow_dispatch vẫn hỗ trợ; tag phải khớp VERSION.
- GitHub Actions kiểm chứng native build và thu ảnh UI. Có thể chạy core checks,
  asset checks tại máy dev; giới hạn môi trường khác nhau nên không xem build local
  thành công là đủ cho cả hai nền tảng.

## 5. Nhật ký lỗi từ test thực tế (đừng lặp lại)

### v0.1.0 → v0.1.1 (đã sửa, commit `577617d`)
| Triệu chứng (user test máy thật) | Nguyên nhân gốc | Cách sửa |
|---|---|---|
| Nhập đúng mã mà điện thoại báo máy tính offline | Lệch tên trường presence (`ts` vs `lastSeen`) | Android đọc `ts` + cờ `online` |
| Gửi/test chỉ thấy bong bóng + hạt, không thấy sticker | GIF là file rời, Release chỉ có exe | Nhúng GIF vào exe (EmbeddedResource) |
| Tin nhắn không được xoá khỏi inbox | Xoá theo UUID thay vì pushKey | Dùng pushKey làm định danh event |
| Spam sticker delay 2–3s/cái | Mỗi lần hiện 5s + cửa sổ combo tính từ lúc bắt đầu hiện + hàng đợi nối tiếp | Hiện 2,8s; combo tính từ lần chạm gần nhất; khác cảm xúc thì nhường chỗ sau 1s |
| RAM 133MB | Decode sẵn cả 24 GIF lúc khởi động | Chỉ giữ bytes nén; decode lười, cache ≤6 |
| APK không có icon | Manifest không khai báo icon | Thêm bộ icon Bubu (mọi density + adaptive) |
| CI APK đỏ 11s (trước đó) | `gradle-wrapper.jar` tự lắp bị hỏng (thiếu class) | Thay jar chính thức Gradle 8.7 (kiểm chứng sha256) |

### v0.1.1 → v0.1.2 (đã phát hành)
- User: mọi thứ hơi nhỏ → overlay phóng to ~1,3× (sticker 200→264 DIP, cửa sổ 380×580).
- User (dark mode): sticker chìm trên nền desktop tối → thêm quầng sáng hồng sau sticker
  + `DropShadowEffect` màu hồng sáng thay bóng tối; `BitmapScalingMode=HighQuality`.
- User: tiếng pop không "đã" khi spam → 3 tầng tiếng (`pop.wav`, `pop2.wav`, `pop3.wav`
  nhúng sẵn): combo Lv2 cao/giòn hơn, Lv3 cao nhất + lấp lánh; `PlayPop(tier)`,
  mỗi lần bấm phát lại ngay.
- User: RAM vẫn cao (180MB khi sticker đã hiện) → sau khi overlay ẩn: gỡ animated
  source, `StickerService.ReleaseDecoded()`, `MemoryTrim` (GC + `EmptyWorkingSet`,
  throttle 20s, gọi lúc idle sau khởi động + sau mỗi lần ẩn). Lưu ý trung thực: WPF +
  .NET runtime có "sàn" RAM riêng; muốn thấp hơn hẳn phải đổi stack hiển thị.
- Kho sticker: soi thủ công cả 24 file, phát hiện hàng trà trộn — `hug-01` là gấu mèo,
  `cheer-02` là mèo, `miss-02` fan-edit đầy watermark (đã xoá), `goodnight-02` thực ra
  là pose ôm (chuyển sang `hug-03`). Đã bổ sung 8 biến thể xịn (Tenor/GIFDB, xử lý
  trong suốt + kiểm chứng alpha góc): kho hiện có **28 sticker**. Việc mở còn lại:
  `tease` mới có 1 mẫu (`tease-02`); một file cười lớn bị xếp nhầm vào cheer đã chuyển
  về `funny-03`; `cheer-03` là GIF ngang nên nhân vật hơi nhỏ khi hiện.

### v0.1.2 → v0.1.3 (đang làm)
- User: tiếng pop vẫn không tạo cảm giác spam → **thay toàn bộ engine tiếng**:
  bỏ SoundPlayer (chỉ 1 tiếng tại một thời điểm, spam là cụt) → `SoundService`
  với **thang 10 nốt** `pop01..pop10.wav` (cao dần theo độ dài combo), mỗi nốt một
  `MediaPlayer` riêng nên tiếng **chồng nhau** được; WAV trích ra
  `%AppData%/BuduPing/sounds` lúc khởi động. `App.PlayPop(comboCount)`.
- User: combo phải "càng nhấn càng to + đổi biến thể" → mỗi tap +7% kích thước
  (trần 1.85×), badge xN từ tap 2, **mỗi 4 tap tự đổi sang biến thể khác cùng cảm xúc**.
- User: quầng sáng chưa cứu được nền tối → đặt **thẻ nền trắng-hồng mờ 95%** sau
  sticker (kiểu thẻ thông báo) — giải pháp chắc chắn cho mọi wallpaper.
- User: chữ tin nhắn + badge xN nhỏ → BubbleText 20, header 16, badge 24.

## 6. Chuẩn nghiệm thu của user (không được phá)

- Mượt kiểu Telegram: bấm là phản hồi ngay, sticker hiện <1s, animation 60fps chỉ dùng
  transform/opacity, nghe Firebase bằng push (không polling), desktop idle ~0% CPU.
- Nhẹ: RAM idle càng thấp càng tốt (mục tiêu gốc <40MB — hiện chưa đạt vì sàn WPF,
  xem mục 5; mọi tối ưu RAM phải giữ app chạy nền tray ổn định).
- **CẤM** tính năng "gợi ý theo giờ" (bản prototype web từng có — user đã loại khỏi bản thật).
- Desktop phải có công tắc bật/tắt tiếng hiệu ứng (settings + menu tray, mặc định bật).
- Hiệu ứng chỉ quanh sticker góc phải dưới — KHÔNG pháo giấy/hiệu ứng toàn màn hình.
- Desktop: X = thu xuống tray, thoát hẳn chỉ qua menu tray; có tuỳ chọn chạy cùng Windows.
- Điện thoại: UI tối giản, không chữ hướng dẫn dài; trạng thái kết nối là 1 chấm presence
  + pill mã cặp (bấm pill = huỷ ghép nối); người gửi tự đặt tên hiển thị.

## 7. Chuẩn chọn sticker (khi thêm/bớt file trong `Assets/Stickers`)

- Bắt buộc đúng nhân vật Yier (panda trắng, tai nâu) / Bubu (gấu nâu) của 一二布布.
  Đã từng lẫn gấu mèo, mèo, fan-edit — phải soi frame đầu trước khi nhận.
- Ưu tiên ≥320px, ≥6 frame (GIF 2 frame gần như tĩnh, nhìn rẻ), không watermark,
  không banner chữ to. Nền trong suốt bằng `tools/make_transparent.py` (flood-fill từ
  mép), kiểm chứng alpha ở 4 góc = 0 bằng PIL sau khi xử lý.
- Đặt tên `<type>-NN.gif` trong thư mục đúng cảm xúc; file được nhúng tự động nhờ glob
  trong csproj, không cần sửa code khi thêm sticker.

## 8. Việc còn mở (cập nhật 0.2.0)

- Hotkey Ctrl+Alt+Space đã có trong 0.2.0; còn tự phát hiện fullscreen,
  giờ yên lặng (quiet hours), nhúng font Baloo 2 (hiện dùng font hệ thống).
- Đánh giá lại RAM sau v0.1.2 bằng số đo của user; nếu vẫn chưa đạt kỳ vọng
  "siêu nhẹ" thì cân nhắc viết lại lớp overlay bằng Win32/GDI thuần.
- Sticker nguồn độ phân giải cao hơn (hiện đa số 250–320px, phóng to trên màn
  HiDPI vẫn hơi mềm).

## 9. Bàn giao 0.2.0

- Báo cáo chi tiết: [DEVELOPMENT-REPORT-0.2.0.md](DEVELOPMENT-REPORT-0.2.0.md).
- Quan trọng: cache nay chỉ nhả sau idle 20s; không gọi EmptyWorkingSet/GC cưỡng bức.
- Nguồn sticker chuẩn duy nhất: Windows Assets/Stickers. Chạy `python tools/prepare_assets.py` trước cả hai build.
- Inbox chỉ xoá sau khi hiển thị hoặc sau quyết định expired/superseded; completion PATCH và delivery status đi cùng nhau.
- Có hotkey/tray đáp lại và feedback Android. Không có notification nền Android.
- UI Android theo bố cục prototype 4×3, không có ô gợi ý vàng/gợi ý theo giờ.
