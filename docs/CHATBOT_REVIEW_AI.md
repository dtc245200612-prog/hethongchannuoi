# Nhật ký review và cải tiến khi bổ sung chatbot bằng trợ lý lập trình AI

**Bối cảnh:** mã gốc chỉ có phân hệ Reports.Generate với đầu ra bản nháp (đã kiểm tra người duyệt) mà **chưa có giao diện hội thoại**. Trong lần bổ sung này, trợ lý lập trình AI đã đọc các tệp `Program.cs`, `Services/ReportService.cs`, `Services/BusinessException.cs`, `Data/FarmDbContext.cs`, `_Layout.cshtml`, so sánh với 10 tiêu chí TX3, và thực hiện các sửa đổi cụ thể sau.

| Phát hiện khi review | Rủi ro | Cải tiến trong phiên bổ sung |
|---|---|---|
| AI trước đây chỉ tạo report, không có luồng chat | Không thể đặt câu hỏi tự nhiên | Thêm `ChatController`, `ChatService`, hai giao diện chat |
| Trả HTML AI nếu render trực tiếp | XSS khi nội dung model không đáng tin | `chat.js` chỉ sử dụng `textContent` và tạo anchor cho đường dẫn nội bộ xác định từ backend |
| Câu hỏi tự mang role/ID có thể vượt quyền | Rò số liệu chuồng của người khác | `ChatContextService` áp dụng `AccessScope.VisibleHerds(User)` trước mọi truy vấn, không nhận ID/role trong body |
| Model có thể gán mã nguồn không thuộc snapshot | Dẫn nguồn giả/hallucination | `ChatSafety.ValidateModelJson` chỉ chấp nhận ID nguồn được server tạo, sai là 502 |
| Chat gọi API lâu và response không kiểm kích cỡ | Treo giao diện/cạn bộ nhớ | 30s deadline, 128 KB HTTP response, rate limiter và thông báo lỗi có mã |
| Lộ API key nếu đặt ở frontend | Rò khóa | Đọc `OPENAI_API_KEY` ở `ChatModelClient` backend; Ollama URL ở cấu hình máy chủ |
| Thiếu kiểm thử riêng cho chat | Không có regression check | Thêm xUnit `ChatSafetyTests` + danh sách manual cases |
| Prompt nhúng vào controller khó so sánh | Không đánh giá cải tiến | Tách ba system prompt + user prompt, thêm script đo 18 ca |

**Kết quả review:** có thay đổi mã tương ứng từng hạng mục ở bảng trên; đây là chứng cứ review/refactor bằng AI trong quá trình tạo gói, **không phải chứng nhận đã chạy test bảo mật**. Trước triển khai thực tế cần code review độc lập, chạy tests, kiểm thử đồng thời và kiểm tra hiệu năng.
