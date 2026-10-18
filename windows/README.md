# BuduPing — Windows Overlay (C# .NET 8 WPF)

App desktop của dự án BuduPing: overlay trong suốt ở góc phải dưới, nhận event cảm xúc
từ Firebase Realtime Database và hiện sticker Yier & Bubu kèm bong bóng chat.

## Build (trên máy Windows hoặc GitHub Actions)

```powershell
cd windows/BuduPing
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o publish
```

→ `publish/BuduPing.exe` (bấm-là-chạy, không cần cài .NET).
Workflow `.github/workflows/build-windows.yml` tự build khi push.

## Cấu trúc

```
BuduPing/
├── BuduPing.csproj
├── App.xaml(.cs)            # khởi động: config → tray → sync → presence; điều phối chung
├── Core/
│   ├── PairCode.cs          # mã cặp Crockford 7+1 checksum, Y HỆT docs/pair-code.md
│   ├── PingEvent.cs         # model event { id, type, text, from, ts }
│   └── AppConfig.cs         # %AppData%/BuduPing/config.json + autostart (HKCU\...\Run)
├── Services/
│   ├── FirebaseSync.cs      # REST + SSE streaming /pairs/{id}/inbox (push, không polling),
│   │                        # dedupe theo id, DELETE sau khi hiện, presence heartbeat 20s
│   └── StickerService.cs    # preload/decode GIF lúc khởi động, random chống trùng,
│                            # map type → nhãn TV + loại hạt
├── Ui/
│   ├── OverlayWindow.*      # overlay click-through (WS_EX_TRANSPARENT), queue 3,
│   │                        # combo 1.5s (Lv2=x3, Lv3=x5+), hạt cục bộ pool 24,
│   │                        # spring 0.6→1.08→1 (260ms), ẩn sau 5s
│   └── SettingsWindow.*     # mã cặp to + Copy/Gen mới, công tắc tiếng + autostart, Test sticker
└── Assets/
    ├── tray.ico / Sounds/pop.wav   # nhúng vào assembly
    └── Stickers/<type>/*.gif       # teammate thả GIF trong suốt vào đây
```

## Luồng chạy

1. Lần đầu mở → tự gen mã `XXXX-XXXX`, lưu `%AppData%/BuduPing/config.json`.
2. `FirebaseSync` mở 1 SSE stream tới `/pairs/{pairId}/inbox.json` (push thật, idle ~0% CPU),
   tự reconnect backoff 1s→30s khi rớt mạng.
3. Event mới → dedupe theo `id` → overlay hiện sticker + bong bóng (tối đa 80 ký tự) →
   DELETE khỏi inbox → heartbeat presence mỗi 20s.
4. Combo: cùng `type` trong 1.5s → gộp (x3 = to 1.3x + badge, x5+ = bùng nổ cục bộ:
   sticker lớn + 3 mini chạy nối đuôi + mưa hạt quanh sticker).
5. X cửa sổ cài đặt chỉ ẩn xuống tray; thoát hẳn qua menu tray (xoá presence → chấm xám).

## Chưa làm / lưu ý

- Chưa có: đáp lại bằng phím tắt (spec mục 8.1), peek mode khi fullscreen, giờ yên lặng —
  theo thứ tự ưu tiên V1, để bản sau.
- Font bong bóng dùng font hệ thống (spec gợi ý Baloo 2 — chưa nhúng font).
- Chưa test build trên Windows (môi trường dev là Linux); build đầu tiên nên chạy qua
  GitHub Actions để bắt lỗi sớm.
