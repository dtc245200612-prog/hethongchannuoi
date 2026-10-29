# Sửa menu bên trái cố định khi cuộn

## Nguyên nhân
Bản trước đặt `.shell` là `min-height: calc(100vh - 70px)` và không giới hạn
vùng cuộn cho `.main`, khiến chiều cao của trang tăng theo nội dung. Kết quả:
khi lăn chuột cả menu và nội dung cùng di chuyển.

## Đã sửa
- `body` có chiều cao cửa sổ (`100dvh`) và `overflow: hidden`.
- `.shell` chiếm chiều cao còn lại bên dưới header với `min-height: 0`.
- `.main` có `overflow-y: auto` để cuộn riêng.
- `.sidebar` có vùng cuộn riêng chỉ khi menu cao hơn cửa sổ.
- Không thay đổi controller, model, cấu hình OpenAI hoặc cơ sở dữ liệu.

## Cách kiểm tra
1. Chạy `FarmAI.sln` bằng Visual Studio và đăng nhập.
2. Mở **Cân đo, tăng trưởng** hoặc trang có nhiều dòng dữ liệu.
3. Đặt con trỏ chuột vào nội dung bên phải và lăn xuống.
4. Thanh điều hướng bên trái và tiêu đề FarmAI phải đứng yên.
5. Thu nhỏ chiều cao trình duyệt; có thể cuộn riêng menu khi cần.
6. Nhấn `Ctrl + F5` trong trình duyệt nếu vẫn thấy CSS cũ.
