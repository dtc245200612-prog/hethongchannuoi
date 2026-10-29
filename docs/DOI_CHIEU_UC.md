# Đối chiếu 30 Use Case theo báo cáo

Nguồn: tài liệu đính kèm, Chương 2, trang in 21–30 và từ điển dữ liệu trang in 41–44. Từng mã UC được giữ nguyên. **Đã triển khai** nghĩa là có luồng UI+service+DB cho kịch bản chính; **Một phần** nghĩa là còn nhánh theo báo cáo chưa hiện thực đầy đủ. Không dùng đây làm bằng chứng các test đã chạy.

| Use Case | Tình trạng | Thành phần/giới hạn |
|---|---|---|
| UC-01 Đăng nhập | Đã triển khai | Account/Login, cookie 15 phút; không phải JWT của báo cáo |
| UC-02 Quản lý phiên | Một phần | Thu hồi phiên khi Logout, kiểm tra phiên/role mỗi request; thiếu refresh rotation |
| UC-03 Quản trị tài khoản | Đã triển khai | Admin, không vô hiệu hóa manager cuối |
| UC-04 Quản lý chuồng | Đã triển khai | Barns, kiểm tra sức chứa và trạng thái |
| UC-05 Quản lý đàn | Đã triển khai | Tạo+nhập+allocation nguyên tử, xem/lọc/phân trang; sửa mô tả, lưu trữ đàn rỗng |
| UC-06 Chuyển chuồng | Đã triển khai | Transfer, đổi phân bổ, không đổi quantity |
| UC-07 Chia đàn | Một phần | Split giữa 2 đàn có sẵn; chưa tạo đàn đích trong cùng form |
| UC-08 Hao hụt/điều chỉnh | Đã triển khai | RequestMovement/ApproveMovement và audit |
| UC-09 Nhật ký thức ăn | Đã triển khai | Care: FEED, null nếu thiếu giá |
| UC-10 Nhật ký thuốc | Một phần | Care: MEDICINE bắt buộc căn cứ; chưa catalog ngừng thuốc |
| UC-11 Xác nhận nhật ký | Đã triển khai | Care.Approve, chặn tự duyệt |
| UC-12 Cân đo | Đã triển khai | Growth/Create lưu sample, weight, method |
| UC-13 Xem tăng trưởng | Đã triển khai | ADG null nếu thiếu cơ sở, xem chuỗi |
| UC-14 Sinh sản | Một phần | Ghi sự kiện và nhập đàn nguyên tử, chống lặp; chưa bản nháp |
| UC-15 Lập lịch tiêm | Đã triển khai | Plan, điều chỉnh/hủy có audit |
| UC-16 Xác nhận lần tiêm | Đã triển khai | Event pending + Approved và target cap |
| UC-17 Cảnh báo | Đã triển khai | Worker, chống trùng theo lịch/người/ngày/loại |
| UC-18 Nhắc bằng AI | Một phần | Task reminder, ngữ cảnh lịch, mẫu fallback; chưa thông báo AI gửi chủ động |
| UC-19 Lập phiếu bán | Đã triển khai | Draft + lines, không làm giảm tồn |
| UC-20 Xác nhận bán | Đã triển khai | Serializable transaction, idempotency, movement/audit |
| UC-21 Thanh toán | Đã triển khai | Payment kiểm tra số dư, revenue ≠ receipts |
| UC-22 Dashboard | Một phần | Chỉ số theo kỳ và phạm vi, không có biểu đồ đầy đủ/mortality opening lịch sử |
| UC-23 Tóm tắt đàn AI | Một phần | Generate task herd_summary; prompt chung, cần đánh giá ca |
| UC-24 Báo cáo tăng trưởng AI | Một phần | Snapshot nguồn, ADG, AI adapter, draft; cần benchmark |
| UC-25 Phê duyệt AI | Đã triển khai | Phiên bản, người duyệt, audit và cảnh báo |
| UC-26 Kiểm tra quyền | Đã triển khai | AccessScope + auth cookie, nghiệp vụ kiểm tra tiếp |
| UC-27 Kiểm tra số lượng | Đã triển khai | FarmRules/transaction/capacity; cần test stress đồng thời SQL Server |
| UC-28 Thu thập báo cáo | Một phần | Snapshot tối thiểu theo đàn và kỳ, có SHA256; chưa kỳ chốt global |
| UC-29 Kiểm tra AI | Một phần | JSON/source/missing hậu kiểm; chưa kiểm định ngữ nghĩa tự động đủ mạnh |
| UC-30 Audit | Đã triển khai | Audit nghiệp vụ quan trọng, lưu cùng SaveChanges |

### Ranh giới thay đổi so với tài liệu

Báo cáo mô tả FastAPI, React, PostgreSQL, Argon2id, JWT + refresh token, Docker và Ollama. Gói đáp ứng yêu cầu người dùng chuyển qua Visual Studio 2026 Insiders bằng .NET 10 MVC, Razor, EF Core và SQL Server; không sao chép nguyên các đoạn Python trong báo cáo. Toàn bộ dữ liệu seed là giả định và không phải chứng cứ kiểm thử thực tế.


### Bổ sung chatbot (ngoài 30 UC gốc)

- `CHAT-01`: người dùng đăng nhập đặt câu hỏi tự nhiên, có lịch sử ngắn chỉ ở trình duyệt.
- `CHAT-02`: backend trích ngữ cảnh SQL Server đã lọc quyền, số liệu chỉ đọc.
- `CHAT-03`: Ollama/OpenAI adapter trả JSON có source_ids và hạn chế, không đủ nguồn thì từ chối.
- `CHAT-04`: có fallback RULE_BASED khi chưa cấu hình model, không giả là AI thật.
- `CHAT-05`: kiểm soát anti-forgery, rate limit, timeout, XSS, prompt injection; không cung cấp chỉ định thú y.

Đối chiếu 10 tiêu chí TX3 trong `CHATBOT_AI_TIEU_CHI_TX3.md`.
