# Tạo Firebase cho BuduPing (làm 1 lần, ~5 phút)

Chốt hạ tầng: **Firebase Realtime Database (gói Spark, miễn phí)** — không phải Cloudflare.
(App cặp đôi theo mã cặp: ai biết mã `XXXX-XXXX` mới vào được đúng cặp của mình — đó là cơ chế của file rules này.)

## Các bước trên firebase.google.com (đăng nhập bằng Google của bạn)

1. Vào https://firebase.google.com → **Get started / Go to console** → **Create a project**.
2. Tên project: `BuduPing` → Continue.
3. Tắt **Google Analytics** cho gọn → **Create project** → đợi tạo xong → Continue.
4. Menu trái: **Build → Realtime Database → Create Database**.
   - Location: chọn **asia-southeast1 (Singapore)** — gần Việt Nam nhất.
   - Security rules: chọn **Start in locked mode** → Enable.
5. Vào tab **Rules** của Realtime Database → xoá hết nội dung cũ → dán nội dung file `firebase/database.rules.json` trong repo này → **Publish**.
6. Đăng ký app Android: ở màn hình Project Overview → bấm icon **Android** (</> là web, chọn đúng icon robot).
   - Android package name: `com.buduping.app`
   - App nickname: `BuduPing Android` → **Register app** → tải file **google-services.json** về.
7. Quay lại tab **Data** của Realtime Database, copy dòng URL của database (dạng `https://buduping-xxxx-default-rtdb.asia-southeast1.firebasedatabase.app`).

## Gửi lại cho Muse

- File **google-services.json** (đính kèm trong chat — file này sinh ra để nhúng vào app, không phải mật khẩu).
- Dòng **Database URL** copy ở bước 7.

Nhận đủ 2 thứ này là Muse nối code vào và build bản đầu tiên.
