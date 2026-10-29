# Bật OpenAI cho FarmAI trong Visual Studio Community 2026 Insiders

## Bước 1 – Bảo vệ khóa API

Nếu khóa từng xuất hiện trong chat, ảnh hoặc commit, hãy **thu hồi khóa đó tại https://platform.openai.com/api-keys và tạo khóa mới**. Không dùng lại khóa đã lộ. OpenAI Platform API tính phí riêng so với ChatGPT.

Không thêm API key vào appsettings.json, launchSettings.json, các file CS/CSS/JS hoặc README.

## Bước 2 – Khởi tạo cấu hình bí mật riêng trên máy của bạn

Giải nén dự án, mở `FarmAI.sln` bằng Visual Studio. Ở Solution Explorer, nhấn chuột phải lên **FarmAI** (không phải solution hoặc FarmAI.Tests) → **Manage User Secrets**. Visual Studio sẽ mở file `secrets.json` dành riêng cho máy và người dùng của bạn. Dán **khóa MỚI** vào đó:

```json
{
  "OPENAI_API_KEY": "PASTE_NEW_KEY_ON_YOUR_PC_ONLY"
}
```

Đây là file bên ngoài dự án và KHÔNG nằm trong ZIP. Không chia sẻ ảnh chứa khóa và không đưa file secrets.json lên GitHub.

## Bước 3 – Chọn chế độ OpenAI

Bản nâng cấp đã có cấu hình mặc định trong `FarmAI/appsettings.json`:

```json
"AI": {
  "ChatProvider": "OpenAI",
  "ChatModel": "gpt-4.1-mini",
  "ReportProvider": "OpenAI",
  "ReportModel": "gpt-4.1-mini"
}
```

Tùy chỉnh tên model nếu dự án OpenAI của bạn không có quyền sử dụng model này; không thay đổi endpoint tại frontend.

## Bước 4 – Chạy web

Cần .NET 10 SDK, LocalDB hoặc SQL Server. Chọn profile **FarmAI (Kestrel)** → **Ctrl+F5**. Ở môi trường Development, `WebApplication.CreateBuilder` sẽ nạp User Secrets của dự án. Nếu trước đó chương trình đã chạy, hãy dừng hoàn toàn và chạy lại.

Đăng nhập `quanly` với `FarmAI@2026` trong môi trường demo.

## Bước 5 – Kiểm thử chatbot thật

1. Chọn menu **Trợ lý chatbot AI** hoặc nhấn nút **✦ Trợ lý AI** góc dưới màn hình.
2. Kiểm tra dòng trạng thái đã cấu hình **OPENAI**, thay vì `RULE_BASED`.
3. Hỏi: “Hiện có bao nhiêu đàn và bao nhiêu con trong phạm vi tôi quản lý?”
4. Hỏi tiếp: “Đàn nào có lần cân gần đây? Có thể so sánh ADG không?”
5. Hỏi: “Lịch tiêm nào quá hạn và còn bao nhiêu con chưa được xác nhận tiêm?”
6. Mở **Báo cáo và AI**, tạo dự thảo và kiểm tra nhãn `OPENAI` cùng các nguồn. Phê duyệt chỉ sau khi đã đối chiếu dữ liệu.

Nếu xuất hiện `RULE_BASED`, ứng dụng chưa nhận được khóa API mới. Nếu lỗi `INVALID_API_KEY` hoặc HTTP 401/403, kiểm tra quyền project và khóa. Nếu 429, cần kiểm tra hạn mức/tốc độ hoặc thanh toán trên OpenAI Platform. Nếu phản hồi sai schema, thử hỏi ngắn hơn hoặc đổi model được hỗ trợ. Không gửi khóa qua chat để nhờ kiểm tra.

**Không cam kết thông minh tương đương ChatGPT web:** chất lượng phụ thuộc tên model, dữ liệu nguồn, hạn mức và cách đặt câu hỏi. AI không được kê đơn, tự quyết định tiêm hoặc thực hiện nghiệp vụ ghi dữ liệu.
