# CHATBOT AI — Đối chiếu 10 tiêu chí Bài kiểm tra thường xuyên số 3

Tài liệu minh chứng dựa trên **mã nguồn** trong thư mục FarmAI. Không biến ca kiểm thử được viết thành kết quả đã chạy. Môi trường sinh mã không có .NET 10 SDK, nên `dotnet build`, `dotnet test` và kiểm thử máy người dùng phải thực hiện trong Visual Studio.

| Tiêu chí | Minh chứng trong mã | Tình trạng/giới hạn |
|---|---|---|
| 1. AI chạy trong hệ thống | Menu `Trợ lý chatbot AI`, widget nổi trong `Views/Shared/_Layout.cshtml`, `ChatController` | Đã tích hợp UI và backend, cần bật model để có AI thật |
| 2. Gọi model/API, bảo vệ khóa | `Services/ChatModelClient.cs`, `Services/OpenAiReportClient.cs`, OpenAI `/v1/chat/completions` hoặc Ollama `/api/chat` | API key chỉ đọc `OPENAI_API_KEY` phía server; không nằm trong JS/View/config mẫu |
| 3. Prompt có hệ thống | `FarmAI/Prompts/chat_system_v3.txt`, `chat_user_template.txt` | system/user riêng, JSON `answer`, `source_ids`, `limitations` |
| 4. Tối ưu qua >=3 vòng | `chat_system_v1.txt` → `v2.txt` → `v3.txt`, `scripts/evaluate_chat_prompts.py` | Có thiết kế so sánh cùng dữ liệu và bảng xuất CSV thực tế; **chưa chạy 3 vòng model trong gói ZIP** |
| 5. Sử dụng dữ liệu & kiểm soát quyền | `ChatContextService` + `AccessScope.VisibleHerds`, query giới hạn dòng, tiến độ tiêm và ADG mẫu | Số lượng/lịch/cân/nhật ký đã duyệt từ DB theo role và chuồng, không gửi mật khẩu/thông tin khách |
| 6. Hiển thị kết quả & cảnh báo | `wwwroot/js/chat.js`, `_ChatInterface.cshtml` | Phản hồi dạng text an toàn, nhãn provider, nguồn, hạn chế, thông báo thú y |
| 7. Xử lý lỗi/giới hạn AI | `ChatModelClient`, `ChatSafety`, `Program.cs`, `ChatController` | 30 giây, giới hạn 128 KB, kiểm schema/source ID, retry tối đa 1 lần lỗi tạm thời, rate limit 8 lần/giờ/tài khoản (dùng chung policy AI) |
| 8. Kiểm thử | `FarmAI.Tests/ChatSafetyTests.cs`, `FarmAI.Tests/OpenAiContractsTests.cs`, `docs/CHATBOT_KIEM_THU.md` | Có unit test và danh sách manual test, chưa có kết quả chạy xác nhận |
| 9. Review/refactor bằng AI | `docs/CHATBOT_REVIEW_AI.md` | Ghi các vấn đề đã kiểm tra và sửa trong lần bổ sung; chưa có nhật ký benchmark hay review độc lập |
| 10. Trải nghiệm nhất quán | Menu + widget nổi + trang riêng, gợi ý câu hỏi, nút xóa, Enter gửi, Shift+Enter xuống dòng | Không yêu cầu người dùng rời website; đang chờ kiểm tra thực tế UX trên trình duyệt |

**Ranh giới:** chatbot chỉ đọc dữ liệu, không có công cụ sửa DB. Không được dùng như lời khuyên điều trị; chống prompt injection bằng phân tách system/user và hạn chế nguồn nhưng không tuyên bố ngăn tuyệt đối hallucination. Lịch sử chat không lưu vào CSDL: chỉ giữ tạm trong JS khi trang còn mở. Bộ lọc quyền ở backend, không tin role do client gửi. Không đưa dữ liệu trang trại thật lên cloud AI nếu chưa được chủ dữ liệu cho phép.

## 3 vòng tối ưu prompt (thiết kế cần đo)

- **V1:** trợ lý trả lời tự do, mục tiêu quan sát lỗi không trích nguồn hoặc bịa số; đây là đối chứng.
- **V2:** tách system/user, ép JSON + source_ids + an toàn thú y; mục tiêu cải thiện tính cấu trúc.
- **V3:** thêm giới hạn dữ liệu, hướng dẫn chống prompt injection, phân quyền, chỉ dùng nguồn server; hậu kiểm ở `ChatSafety.ValidateModelJson`.

Chạy thật trên máy có Ollama và model đã tải:

```powershell
ollama pull llama3.2:3b
python scripts/evaluate_chat_prompts.py --model llama3.2:3b
```

Script gửi **6 tình huống giả lập x 3 prompt = 18 yêu cầu** đến Ollama, đo độ trễ, JSON hợp lệ và source ID; ghi `docs/prompt_eval_results.csv`. Các cột `manual_factuality`, `manual_vet_safety` ban đầu là `NOT_REVIEWED`: người đánh giá phải đọc phản hồi thực tế mới chấm. Không tuyên bố V3 tốt hơn khi chưa đo. Chi tiết xem `CHATBOT_KIEM_THU.md`.

**Cấu hình OpenAI cho cả Chatbot và Báo cáo:** `docs/CAI_OPENAI_TRONG_VISUAL_STUDIO.md`. Khóa API đã chia sẻ trong hội thoại cần thu hồi, không được đưa vào code hoặc tệp ZIP. Hai chức năng trả nội dung OpenAI có `source_ids`, báo cáo là DRAFT chờ duyệt.
