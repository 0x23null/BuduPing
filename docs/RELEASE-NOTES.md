# BuduPing v0.2.1 — character stickers, lighter memory

## Mới: chọn nhân vật của bạn
- Lần đầu mở app Android sau khi ghép cặp, app hỏi "Bạn là ai trong hai đứa?" — chọn **Bubu** (gấu nâu) hoặc **Dudu** (gấu trúc trắng). Từ đó sticker bạn gửi chỉ lấy đúng phía của mình: sticker solo của nhân vật bạn chọn + sticker có cả hai, không bao giờ dính sticker solo của phía còn lại. Đổi lại bất cứ lúc nào bằng chip nhân vật trên đầu màn hình chính.
- PC cũng có mục "Bạn là" trong Cài đặt — cái ôm đáp lại bằng Ctrl+Alt+Space sẽ theo nhân vật của người dùng máy.
- Toàn bộ 27 sticker đã được rà soát và gắn thẻ nhân vật theo từng hình.

## Nhẹ RAM hơn
- Bỏ hẳn MediaPlayer/Media Foundation khỏi bản Windows: tiếng pop giờ phát bằng engine PCM siêu nhẹ (waveOut), vẫn chồng tiếng khi spam và leo nốt theo combo.
- Cache giải mã GIF siết còn tối đa 16MB / 4 hình; app tự dọn bộ nhớ khi overlay đã ẩn.

## Giao diện
- Bong bóng tin nhắn trên desktop được vẽ lại: avatar tròn kèm emoji theo cảm xúc, tên người gửi gọn gàng, nhãn cảm xúc dạng pill, font và giãn dòng dễ đọc hơn.
- Bỏ câu khẩu hiệu "Một chạm, một chút thương." và bỏ khung "Xem trước vị trí" trong Cài đặt theo yêu cầu.

## Chất lượng sticker
- Rà soát lại toàn bộ: 100% khung hình có 4 góc trong suốt hoàn toàn, không viền mờ/halo. Gỡ **love-01** vì bị chèn chữ fan-edit ("my motka duduuu…", chữ cuối bị cắt mép) — cùng loại lỗi đã gỡ ở các bản trước.
- Kèm tài liệu `docs/STICKER-TEST-PLAN.md`: kế hoạch test sẵn 5 mood × nền tối/sáng/trắng.

Cài đè lên bản cũ, không cần ghép cặp lại.
