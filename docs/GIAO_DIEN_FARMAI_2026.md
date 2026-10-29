# FarmAI — giao diện Emerald Harvest (13/10/2026)

Phiên bản này tập trung vào trải nghiệm trực quan phù hợp chủ đề chăn nuôi, không thay đổi mã nghiệp vụ, dữ liệu hoặc cách gọi OpenAI.

## Điểm mới

- Trang chủ có banner minh họa trang trại bằng SVG nội tuyến (không tải ảnh từ dịch vụ bên ngoài), hai đường dẫn nhanh đến Quản lý đàn và Trợ lý AI.
- Các KPI vẫn đọc từ `ViewBag.Summary` và danh sách lịch gốc: tổng đàn, vật nuôi, hao hụt, lịch chưa hoàn tất. Không dùng số KPI trang trí cố định.
- Hai bảng đàn/lịch, thông báo và thẻ AI thống nhất bố cục, có trạng thái rỗng để giải thích khi chưa có dữ liệu.
- Menu nhóm theo Tổng quan / Vận hành trang trại / Thông minh & báo cáo / Quản trị. Menu tự đánh dấu trang đang mở và giữ nguyên phân quyền từ bản cũ.
- Trên desktop: topbar giữ vị trí, menu trái và nội dung chính có thanh cuộn độc lập (không cuộn cùng nhau).
- Trên điện thoại/tablet: nút menu mở sidebar dạng drawer có lớp nền che, hỗ trợ chạm bên ngoài và nhấn Escape để đóng.
- Biểu mẫu, bảng, nút bấm, thông báo, login và hộp chat dùng tông xanh rừng, xanh lá, kem và vàng rơm; tương thích màn hình nhỏ, hỗ trợ focus bằng bàn phím và reduced motion.

## Các file được thay đổi

- `FarmAI/Views/Shared/_Layout.cshtml`: topbar, logo, biểu tượng SVG, menu động, avatar, chatbot nổi.
- `FarmAI/Views/Home/Index.cshtml`: dashboard mới với dữ liệu thật từ controller hiện có.
- `FarmAI/wwwroot/css/site.css`: toàn bộ style theo chủ đề.
- `FarmAI/wwwroot/js/layout.js`: mở/đóng menu trên màn hình nhỏ.

## Cách cập nhật dự án hiện có

1. **Sao lưu** thư mục dự án đang chạy và file cấu hình do bạn đã chỉnh.
2. Giải nén ZIP mới; mở `FarmAI.sln`.
3. Nếu bạn muốn giữ project cũ, chỉ sao chép bốn file trên theo đúng đường dẫn, không ghi đè các file `appsettings.json`, cơ sở dữ liệu hoặc User Secrets đang dùng.
4. Chạy app bằng Visual Studio, nhấn `Ctrl + F5` trong trình duyệt để cập nhật cache CSS/JS (profile Kestrel vẫn giữ nguyên).
5. Kiểm tra nút menu trên điện thoại (hoặc F12 → Device Toolbar) và nội dung bên phải khi cuộn.

## Phạm vi đã kiểm tra

- Kiểm tra trình duyệt Chromium trên bản HTML mẫu với viewport **1440×900**, **1024×768** và **390×844**: vùng `.main` cuộn độc lập, phần tử sidebar không thay đổi vị trí và body không tạo cuộn toàn trang.
- Kiểm tra menu mobile mở/đóng, lớp nền che và phím Escape trên bản HTML mẫu.
- Các số liệu trong **ảnh xem trước minh họa** không phải kết quả truy vấn hệ thống đang chạy; khi đăng nhập, controller đưa số thực từ database vào trang chủ.
- **Chưa kiểm chứng `dotnet build` hoặc giao diện render bằng Razor thực tế** trong môi trường đóng gói do không có .NET SDK. Cần chạy `dotnet build` và kiểm tra trực tiếp trên Visual Studio.
