# BuduPing

App cặp đôi: điện thoại bấm 1 cảm xúc (nhớ / yêu / ghét / giận...) → sticker **Yier & Bubu** (tên quốc tế: Bubu & Dudu) nảy lên ở góc phải dưới màn hình máy tính Windows, kèm bong bóng tin nhắn.

- **Windows overlay:** C# .NET 8 WPF — trong suốt, click-through, chạy ngầm trong tray, tự khởi động cùng Windows.
- **Android app:** Kotlin + Jetpack Compose — 1 màn hình tối giản, ghép cặp bằng mã `XXXX-XXXX` do app Windows gen ra (không đăng nhập).
- **Cầu nối:** Firebase Realtime Database (gói Spark miễn phí).
- **Build:** GitHub Actions tự build `.apk` + `.exe` — không cần cài Android Studio / Visual Studio.

Trạng thái: đang ở giai đoạn prototype (đã có bản web showcase). Đặc tả đầy đủ nằm ở tài liệu thiết kế của dự án.
