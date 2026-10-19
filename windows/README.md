# BuduPing — Windows (.NET 8 WPF)

Overlay trong suốt, click-through, nhận sticker và tin nhắn từ Android qua Firebase SSE.
Mở EXE có thông báo đã chạy; mở lần nữa đưa cài đặt của tiến trình hiện có lên.
Settings mở lần đầu để lấy mã cặp; nút X thu xuống tray, menu tray mới thoát hẳn.

## Build trên Windows

Từ thư mục gốc repo, chuẩn bị asset và kiểm tra hợp đồng:

```powershell
python -m pip install -r tools/requirements-assets.txt
python tools/prepare_assets.py
python -m unittest discover -s tests/assets -v
dotnet run --project tests/BuduPing.CoreChecks -c Release
dotnet publish windows/BuduPing -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

`publish/BuduPing.exe` chạy độc lập. GIF, catalog, WAV và icon đều nhúng trong assembly.
Workflow `build-windows.yml` thực hiện cùng quy trình và chụp settings native bằng
`BuduPing.exe --ui-review <thư mục>`. Chế độ review không đọc config cá nhân hay kết nối Firebase.

## Điểm vào code

| Thành phần | Vai trò |
| --- | --- |
| `App.xaml.cs` | Tray, sync, presence, reply hotkey, lifecycle và config |
| `Core/PingBacklog.cs` | Hàng đợi bounded, giữ tin có chữ, luật combo/TTL |
| `Core/RecentEventIds.cs` | Dedupe có giới hạn và hỗ trợ retry event đã defer |
| `Services/FirebaseSync.cs` | SSE/reconnect, completion PATCH nguyên tử, phản hồi ôm |
| `Services/StickerService.cs` | GIF nhúng, cache decode theo ngân sách và pin source đang phát |
| `Services/DisplayPlacement.cs` | Monitor, góc, DPI và kích thước overlay |
| `Ui/OverlayWindow.*` | Motion, normalize theo alpha toàn animation, particle pool, giảm chuyển động |
| `Ui/SettingsWindow.*` | Mã cặp, vị trí, riêng tư, tiếng, autostart và trả lời |

Combo chỉ gộp các tap cùng cảm xúc/nhân vật không có chữ và không khoá biến thể trong 2 giây.
Bubu là gấu trắng; Dudu là gấu nâu. Xem `Core/PresentationPolicy.cs` cho giới hạn kích thước, hạt và nhịp âm.
Tin có chữ không bị xoá để nhường chỗ cho spam. Inbox được hoàn tất theo Firebase push key;
`displayed` là đã đưa vào frame hiển thị, không phải người nhận đã đọc.

Tuỳ chọn `BUDUPING_DIAGNOSTICS=1` ghi số đo cục bộ có giới hạn, không chứa mã cặp hay nội dung tin nhắn.
Thông tin wire contract, test máy thật và giới hạn hiện tại nằm trong
[báo cáo bàn giao](../docs/DEVELOPMENT-REPORT-0.2.2.md).

Đo offline không dùng cặp thật: `BuduPing.exe --memory-review <thư mục>`. JSON chứa working set, private bytes, managed heap và idle CPU; so sánh cùng máy và workload, không xem working set sau trim là toàn bộ bộ nhớ cam kết.
