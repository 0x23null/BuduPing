# BuduPing — hướng tương tác "desktop buddy" (từ 2026-10-19)

Cảm hứng: series TikTok "Bubu Dudu desktop buddy" của ANDIE/@andi.o.matic
(93K + 330K lượt xem). Chủ dự án chốt hướng: **sticker siêu đa dạng + siêu
tương tác, thú vị, sáng tạo**. Tài liệu này là la bàn cho các bản 0.3+.

Ranh giới rõ ràng (chủ dự án, 2026-10-19): **chỉ mượn ngôn ngữ HÌNH ẢNH kiểu
pet** (pose bám viền, cảm giác sống thường trực, font/bong bóng cute) — không
sao chép nội dung hay câu chữ của app mẫu (kiểu "You're Back!"). Concept vẫn
là của BuduPing: kênh ping cảm xúc giữa hai người; mọi khoảnh khắc xuất hiện
đều bắt đầu từ một cái chạm của người thương, không phải pet tự nói với chủ.

Nguyên tắc bất biến (luật của chủ dự án, không đổi):
- Không "gợi ý theo giờ"/nhắc việc theo thời gian.
- Mượt như Telegram, nhẹ RAM/CPU; buddy ngồi yên thì CPU ~0% và không giải mã GIF liên tục (dùng frame tĩnh khi idle).
- Chế độ Kín đáo luôn thắng: đang bật kín đáo thì không buddy, không peek.

## Ý tưởng tương tác (xếp theo độ ưu tiên)

1. **Thò ra từ viền (edge peek)** — cảm xúc "hello" mới: sticker trượt ra từ mép
   phải màn hình, thân bị chính viền màn hình cắt một phần, đầu nghiêng ngó vào.
   Cần pose peek/bám mép chuyên dụng (đợt sticker wave 2). Các cảm xúc khác
   vẫn ở thẻ góc phải như hiện tại.
2. **Bong bóng tối + font cute** — tùy chọn bong bóng nền navy đậm, font tròn
   (Baloo 2, có hỗ trợ tiếng Việt) cho lời nhắn, theo đúng ảnh mẫu chủ dự án gửi.
3. **Icon gửi riêng trên điện thoại** — bỏ icon mặc định đen/thuần text; icon
   máy bay giấy/trái tim vẽ riêng, đồng bộ style hồng.
4. **Buddy thường trực (perch mode, tùy chọn)** — một em nhỏ ngồi trên mép trên
   của taskbar khi không có ping; có ping thì bung lên bản lớn như hiện tại.
   Bấm vào em nó → nảy + tim (phản ứng tại chỗ, không gửi đi).
5. **Âm thanh theo cảm xúc** — ngoài thang pop hiện tại: kiss có tiếng "chụt",
   angry phì hơi, funny cười khúc khích… tổng hợp hoặc nguồn CC0, phát qua
   engine waveOut sẵn có.
6. **Kéo thả buddy** dọc mép dưới màn hình; thả ra là ngồi xuống.

## Danh mục sticker đích (master list)

Hiện có 27 (đã rà soát, gắn thẻ nhân vật). Đợt wave 2 săn thêm (~24–30):

| Nhóm | Slot đích | Ghi chú |
|---|---|---|
| Peek/viền | peek-01…06 | ngó từ cạnh, bám mép, đu, vẫy từ sau vật — ưu tiên cao nhất |
| Chào hỏi | hello-01…03 | vẫy tay, nhảy chào — cho cảm xúc mới `hello` |
| Mới | dance, wow, shy, ok, proud, sleepy (mỗi loại 2–3) | cảm xúc mới cho lưới điện thoại |
| Bổ sung | tease +2 (đang chỉ có 1), angry +1, cheer +1 | lấp nhóm mỏng |

Chuẩn chất lượng giữ nguyên: đúng Yier & Bubu, nền trong suốt (kiểm tra alpha
từng frame), không chữ fan-edit/watermark, ≥300px ưu tiên, gắn thẻ nhân vật
(bubu/dudu/cả hai) trước khi vào catalog.

## Lộ trình

- **v0.3**: cảm xúc `hello` + edge peek, bong bóng tối/font cute, icon gửi mới.
- **v0.4**: perch mode + phản ứng khi bấm, âm thanh theo cảm xúc.
- **v0.5**: kéo thả buddy, thêm pose viền cho vài cảm xúc nữa.
