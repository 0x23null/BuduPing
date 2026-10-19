# BuduPing — Kế hoạch test tay: sticker nền trong suốt (v0.2.2)

Mục tiêu: xác nhận sticker hiển thị **sạch, không viền halo, không nền dính trong file GIF** trên desktop ở mọi loại nền, và nhân vật sticker **đúng lựa chọn bubu/dudu** của người gửi.

**Nhận diện chuẩn:** Bubu = gấu trắng; Dudu = gấu nâu. Sticker có cả hai được dùng cho cả hai vai. Dudu chưa có sticker Buồn/Xin lỗi phù hợp nên hai ô này phải báo Chưa có.

## Chuẩn bị (5 phút)

- PC cài BuduPing v0.2.2 + điện thoại đã ghép nối (nếu phải gỡ APK debug do khác chữ ký thì cần ghép lại).
- Trên điện thoại: chọn nhân vật của mình (bubu hoặc dudu) trước khi gửi.
- Trên PC, chuẩn bị 3 nền test:
  1. **Nền tối** — wallpaper tối / đen.
  2. **Nền sáng** — wallpaper sáng / trắng.
  3. **Cửa sổ trắng phía sau** — mở Notepad trắng phủ toàn màn hình rồi để sticker hiện đè lên.

## Các mood cần test

| Mood | Sticker trong mood | Điểm cần soi kỹ |
|---|---|---|
| miss | miss-01, miss-03, miss-04 | miss-01 vs miss-03 gần như trùng nhau — xem có phân biệt được không; miss-04 chỉ có Bubu trắng |
| love | love-02, love-03 | love-01 đã loại khỏi bộ đóng gói; love-02 chỉ Dudu nâu, love-03 chỉ Bubu trắng |
| angry | angry-01, angry-02 | angry-01 **đổi nhân vật giữa chừng** (panda → gấu nâu → panda) + chữ "ANGRY BUBU" dính sẵn trong ảnh |
| tease | tease-02 | chỉ còn đúng 1 variant; cảnh giường giống goodnight-01 — xem có lẫn không |
| goodnight | goodnight-01, goodnight-03 | cả 2 đều có 2 nhân vật |

Với **mỗi mood × mỗi nền**, gửi 1 sticker từ điện thoại và check:

- [ ] Không viền trắng/đen quanh sticker (halo).
- [ ] Không có nền vuông dính vào file GIF. Card sáng native là lớp chủ ý để đọc trên desktop tối, cần phân biệt với nền GIF.
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

- Tất cả 27 GIF đều có 4 góc trong suốt ở mọi frame, không còn sót nền mờ — phần nền đã sạch về mặt kỹ thuật.
- Alpha là dạng cắt cứng (không có vùng bán trong suốt) nên cạnh có thể hơi "gắt" trên nền tối — test bằng mắt là bước quyết định.
- love-01 đã bị loại do watermark. angry-01 có chữ ANGRY BUBU và đổi nhân vật; love-02, cheer-03, sorry-02 cũng có chữ dính sẵn cần xem trên desktop.

## Kiểm tra bổ sung cho 0.2.2

- [ ] Cỡ Vừa khoảng 110 DIP; Nhỏ khoảng 66 DIP; Lớn khoảng 143 DIP (còn nhân theo DPI Windows). Tin nhắn không co chữ theo sticker.
- [ ] Spam 10 lần chậm rồi 100 lần nhanh: badge đếm đúng, nảy có nhịp, accent ở 3/5/10; không nghe hàng dài âm thanh tiếp tục sau khi ngừng gửi.
- [ ] Bật kín đáo rồi bấm Thử sticker và Gửi ôm đáp lại: checkbox còn bật, không phát âm, không lộ lời nhắn. Chỉ Mở xem lời nhắn đang chờ mới tắt chế độ.
- [ ] Tắt tiếng giữa lúc đang phát; không còn phần đuôi âm thanh. Bật giảm chuyển động không xuất hiện hạt.
- [ ] Mở EXE sau setup: thấy thông báo đã chạy, tự ẩn sau khoảng 2.4 giây. Mở lần nữa: chỉ một tiến trình, cài đặt được đưa lên. Khởi động cùng Windows yên lặng.
- [ ] Bật cỡ chữ Android 1.3 ở màn hình 320 dp: chip nhân vật không ép tên thành chữ dọc; tên/trạng thái nằm trong thẻ bên dưới.

Các lớp cần soi riêng: alpha của GIF, card sáng native, shadow/glow native. Đừng đánh dấu card sáng là lỗi mất trong suốt của asset. Đợt kiểm tra tự động 2026-10-19 đọc toàn bộ frame của 27 GIF và kiểm tra 4 góc; halo viền màu và các caption vẫn cần mắt người trên nền thật. Giữ nguyên bản gốc trong khi đánh giá, ưu tiên thử love-03, miss-04, hug-04 trước khi thay hàng loạt.
