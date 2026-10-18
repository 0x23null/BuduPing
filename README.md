# BuduPing

Một chạm trên Android, một sticker Bubu & Dudu xuất hiện trên máy Windows của người thương.
Hai ứng dụng native dùng Firebase Realtime Database và mã ghép cặp `XXXX-XXXX`, không cần đăng nhập.

## Bản 0.2.0 preview

- **Android / Kotlin + Compose:** lưới 12 cảm xúc, hình sticker thật, lời nhắn tuỳ chọn, giữ để chọn biến thể hoặc ghim. Có giao diện sáng/tối và phản hồi ngay khi chạm.
- **Windows / .NET 8 WPF:** sticker nảy vào góc màn hình, combo đổi biến thể, tiếng pop chồng lớp; có tuỳ chọn vị trí, kích thước, giảm chuyển động và ẩn nội dung.
- PC có thể gửi một cái ôm đáp lại bằng `Ctrl+Alt+Space` hoặc menu tray. Điện thoại nhận phản hồi khi đang mở app.
- Trạng thái gửi phân biệt đã ghi lên mạng và đã hiện trên PC. Tin có chữ được giữ riêng khi spam sticker; dữ liệu chưa xử lý vẫn nằm trong inbox.

Đây là bản preview để kiểm tra trên thiết bị thật. Chưa công bố số đo RAM, FPS hay độ trễ mạng.
APK hiện dùng chữ ký debug của CI; nếu không cập nhật đè được bản cũ, cần gỡ bản cũ rồi ghép cặp lại.

## Dùng thử

1. Tải `BuduPing.exe` và `BuduPing.apk` tại [Releases](https://github.com/0x23null/BuduPing/releases).
2. Mở EXE trên Windows, lấy mã cặp trong cửa sổ cài đặt.
3. Cài APK trên Android 8 trở lên, nhập tên và mã đó. Chạm cảm xúc để gửi; nhập lời nhắn trước khi chạm nếu muốn.
4. Đóng cửa sổ cài đặt Windows sẽ thu xuống tray. Dùng menu tray để thoát hẳn.

## Phát triển và build

Kho sticker chuẩn là `windows/BuduPing/Assets/Stickers/`. Trước khi build một trong hai app:

```sh
python -m pip install -r tools/requirements-assets.txt
python tools/prepare_assets.py
python -m unittest discover -s tests/assets -v
```

Android: JDK 17, Android SDK 34, chạy `./gradlew assembleDebug` trong `android/`.
Windows: .NET SDK 8 trên Windows; xem [windows/README.md](windows/README.md).
GitHub Actions build hai nền tảng, chạy kiểm tra core/asset và thu ảnh giao diện native.
Sửa `VERSION` trên main sẽ khởi động pipeline release; APK và EXE chỉ được xuất bản sau khi cả hai build thành công.

## Tài liệu cho người hoặc AI phát triển tiếp

- [DEV-NOTES](docs/DEV-NOTES.md): lịch sử, các lỗi từng gặp và yêu cầu không được phá.
- [Báo cáo bàn giao 0.2.0](docs/DEVELOPMENT-REPORT-0.2.0.md): code đã thay đổi, hợp đồng dữ liệu, kết quả kiểm tra và phần chưa làm.
- [Release notes](docs/RELEASE-NOTES.md), [mã ghép cặp](docs/pair-code.md), [Firebase](docs/firebase-setup.md).
- Nhánh `ci-reports` chứa log build; luôn kiểm tra SHA vì nhiều người/AI có thể build cùng lúc.
