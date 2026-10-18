# BuduPing — Kế hoạch test tay: sticker nền trong suốt (v0.2)

Mục tiêu: xác nhận sticker hiển thị **sạch, không viền halo, không hộp nền** trên desktop ở mọi loại nền, và nhân vật sticker **đúng lựa chọn bubu/dudu** của người gửi.

## Chuẩn bị (5 phút)

- PC cài BuduPing v0.2 + điện thoại đã ghép nối (không cần ghép lại nếu đã cài bản cũ).
- Trên điện thoại: chọn nhân vật của mình (bubu hoặc dudu) trước khi gửi.
- Trên PC, chuẩn bị 3 nền test:
  1. **Nền tối** — wallpaper tối / đen.
  2. **Nền sáng** — wallpaper sáng / trắng.
  3. **Cửa sổ trắng phía sau** — mở Notepad trắng phủ toàn màn hình rồi để sticker hiện đè lên.

## Các mood cần test

| Mood | Sticker trong mood | Điểm cần soi kỹ |
|---|---|---|
| miss | miss-01, miss-03, miss-04 | miss-01 vs miss-03 gần như trùng nhau — xem có phân biệt được không; miss-04 chỉ có dudu |
| love | love-01, love-02, love-03 | love-01 có chữ "my motka duduuu / I love youuuu" bị cắt mép — xem có khó chịu không; love-02 chỉ bubu, love-03 chỉ dudu |
| angry | angry-01, angry-02 | angry-01 **đổi nhân vật giữa chừng** (panda → gấu nâu → panda) + chữ "ANGRY BUBU" dính sẵn trong ảnh |
| tease | tease-02 | chỉ còn đúng 1 variant; cảnh giường giống goodnight-01 — xem có lẫn không |
| goodnight | goodnight-01, goodnight-03 | cả 2 đều có 2 nhân vật |

Với **mỗi mood × mỗi nền**, gửi 1 sticker từ điện thoại và check:

- [ ] Không viền trắng/đen quanh sticker (halo).
- [ ] Không có "hộp" nền mờ/vuông bao quanh nhân vật.
- [ ] Cạnh sticker sạch trên cả nền tối và nền sáng.
- [ ] Nhân vật đúng với lựa chọn trên điện thoại (chọn bubu → chỉ hiện sticker có bubu; chọn dudu → chỉ hiện sticker có dudu; sticker 2 nhân vật hiện bình thường).

## Test thêm

- **Spam test:** bấm 10 cái liên tiếp cùng 1 mood → sticker đổi mượt, không giật, không đơ, badge combo hiện đúng.
- **Tin nhắn text:** gửi sticker kèm chữ → chữ đọc rõ trên card, không bị sticker che.
- **Discreet mode:** bật chế độ kín đáo trên PC → gửi sticker → chỉ hiện peek nhỏ, không phát tiếng, mở tay mới xem được.

## Bảng ghi kết quả

| Mood | Nền | Pass/Fail | Ghi chú |
|---|---|---|---|
| miss | tối | ☐/☐ | |
| miss | sáng | ☐/☐ | |
| miss | cửa sổ trắng | ☐/☐ | |
| love | tối | ☐/☐ | |
| love | sáng | ☐/☐ | |
| love | cửa sổ trắng | ☐/☐ | |
| angry | tối | ☐/☐ | |
| angry | sáng | ☐/☐ | |
| angry | cửa sổ trắng | ☐/☐ | |
| tease | tối | ☐/☐ | |
| tease | sáng | ☐/☐ | |
| tease | cửa sổ trắng | ☐/☐ | |
| goodnight | tối | ☐/☐ | |
| goodnight | sáng | ☐/☐ | |
| goodnight | cửa sổ trắng | ☐/☐ | |
| spam ×10 | — | ☐/☐ | |
| tin nhắn text | — | ☐/☐ | |
| discreet mode | — | ☐/☐ | |

## Tiêu chí "đạt"

Sticker đạt khi: nền trong suốt thật (test bằng cả 3 nền), viền sạch, đúng nhân vật, animation mượt. Ghi vào cột "Ghi chú" bất kỳ hiện tượng nào: viền trắng, hộp nền, chữ lạ, nhân vật sai, giật hình.

## Ghi chú từ đợt audit ảnh (2026-10-19)

- Tất cả 28 GIF đều có 4 góc trong suốt ở mọi frame, không còn sót nền mờ — phần nền đã sạch về mặt kỹ thuật.
- Alpha là dạng cắt cứng (không có vùng bán trong suốt) nên cạnh có thể hơi "gắt" trên nền tối — test bằng mắt là bước quyết định.
- love-01 và angry-01 có chữ fan-edit dính sẵn trong ảnh (xem bảng trên) — nếu thấy khó chịu thì thay sticker.
