# FarmAI ICTU — Hệ thống quản lý chăn nuôi tích hợp AI

Project hoàn chỉnh theo cấu trúc **ASP.NET Core MVC .NET 10**, mở trực tiếp bằng **Visual Studio Community 2026 Insiders** và chạy bằng **Kestrel** (không dùng IIS Express).

## 1. Chức năng đã hoàn thiện

- Đăng nhập và phân quyền 3 vai trò: **Quản lý / Kỹ thuật viên / Nhân viên**.
- CRUD đàn vật nuôi và chuồng trại.
- Nhật ký chăm sóc, lịch chăm sóc sắp tới/quá hạn, đánh dấu trường hợp cần kiểm tra sức khỏe.
- Lịch vaccine, cảnh báo quá hạn / sắp đến hạn, đánh dấu đã thực hiện.
- Theo dõi cân nặng và biểu đồ tăng trưởng.
- Kho thức ăn: tồn kho, mức tối thiểu, nhập / xuất và chống xuất quá tồn.
- Kho thuốc & vật tư: số lượng, mức tối thiểu, hạn dùng. Không có chức năng kê thuốc.
- Theo dõi sinh sản.
- Xuất bán, doanh thu và tự động trừ số lượng đàn.
- Quản lý chi phí.
- Báo cáo: tăng trưởng, doanh thu, chi phí, chênh lệch thu-chi, tồn kho thấp, tỷ lệ hao hụt.
- Trợ lý AI tích hợp trong hệ thống: tóm tắt đàn, nhắc lịch 7 ngày, phân tích tăng trưởng, điểm cần theo dõi.
- Output OpenAI được ràng buộc bằng JSON schema để frontend parse ổn định.
- Có endpoint backend `POST /api/ai/summarize-herd` cho luồng tích hợp sâu.
- Có **Prompt Lab V1/V2/V3** để chạy 3 vòng thử nghiệm trên cùng dữ liệu và ghi log.
- Nhật ký prompt/phản hồi AI phục vụ minh chứng KT3.
- Test tự động cho lịch chăm sóc, lịch vaccine, tăng trưởng, xuất bán, tồn kho, tỷ lệ hao hụt và quyền AI.

## 2. Cách chạy bằng Visual Studio Community 2026 Insiders

1. Cài workload **ASP.NET and web development** và **.NET 10 SDK**.
2. Giải nén project.
3. Mở file `FarmAI.sln`.
4. Visual Studio sẽ tự Restore NuGet. Nếu chưa restore: chuột phải Solution → **Restore NuGet Packages**.
5. Chọn project khởi động **FarmAI**.
6. Trên thanh Run chọn profile **FarmAI** (không chọn IIS Express).
7. Nhấn `Ctrl + F5` hoặc nút ▶.
8. Kestrel chạy tại:
   - `https://localhost:7188`
   - `http://localhost:5188`

Database SQLite `farmai.db` sẽ tự tạo lần đầu và seed dữ liệu mẫu.

## 3. Tài khoản demo

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản lý | `admin` | `admin123` |
| Kỹ thuật viên | `ktv` | `ktv123` |
| Nhân viên | `nv` | `nv123` |

Mật khẩu demo được lưu dưới dạng hash trong database, không lưu plaintext.

## 4. Phân quyền

- **Quản lý**: toàn quyền nghiệp vụ, tài khoản, chi phí, báo cáo, AI.
- **Kỹ thuật viên**: cập nhật dữ liệu chuyên môn, vaccine, tăng trưởng, sinh sản, kho; xem báo cáo và sử dụng AI.
- **Nhân viên**: xem dữ liệu, ghi nhật ký chăm sóc và ghi giao dịch nhập/xuất thức ăn; không gửi dữ liệu sang AI.

## 5. Cấu hình OpenAI API an toàn

Project vẫn chạy khi **chưa có API key** bằng chế độ phân tích nội bộ (fallback). Để dùng AI thật, không ghi key vào `appsettings.json` và không commit key lên Git.

### Cách khuyến nghị trong Visual Studio / terminal

```bash
dotnet user-secrets set "OPENAI_API_KEY" "sk-..." --project FarmAI/FarmAI.csproj
dotnet user-secrets set "OPENAI_MODEL" "gpt-5.6-luna" --project FarmAI/FarmAI.csproj
```

Hoặc đặt biến môi trường `OPENAI_API_KEY` và `OPENAI_MODEL`.

Dịch vụ AI gọi Responses API tại `https://api.openai.com/v1/responses`. Model có thể đổi bằng cấu hình mà không sửa code. Kết quả được yêu cầu theo Structured Output/JSON schema gồm `title`, `summary`, `highlights`, `reminders`, `warnings`.

## 6. Cách reset dữ liệu demo

Dừng ứng dụng, xóa file:

```text
FarmAI/farmai.db
```

Sau đó chạy lại. `DatabaseSeeder` sẽ tạo dữ liệu mẫu mới.

## 7. Chạy test

Trong Visual Studio: **Test → Run All Tests**.

Hoặc terminal:

```bash
dotnet test FarmAI.sln
```

Test nằm trong project `FarmAI.Tests` và tài liệu manual test nằm tại `tests/TEST_CASES.md`.

## 8. Minh chứng Bài kiểm tra thường xuyên 3

| Tiêu chí | Minh chứng trong project |
|---|---|
| 1. AI tích hợp trong hệ thống | `AiController`, trang `Views/Ai/Index.cshtml` |
| 2. Kết nối API/model & bảo vệ key | `Services/OpenAiService.cs`, User Secrets / env |
| 3. Prompt tách khỏi code | `FarmAI/Prompts/prompts.json` + JSON schema trong `OpenAiService` |
| 4. 3 vòng tối ưu prompt | `prompts.v1.json`, `prompts.v2.json`, `prompts.json`, trang `Ai/PromptLab`, `docs/PROMPT_EXPERIMENTS.md` |
| 5. Dùng dữ liệu hệ thống + quyền | `FarmDataService`, `[Authorize(Roles=...)]` |
| 6. Kết quả rõ ràng + cảnh báo | giao diện AI chia Summary/Highlights/Reminders/Warnings |
| 7. Timeout/rate-limit/empty/JSON/long data | `OpenAiService.cs` |
| 8. Kiểm thử đúng/sai/biên | `FarmAI.Tests`, `tests/TEST_CASES.md` |
| 9. Review code bằng AI | `docs/AI_CODE_REVIEW.md` |
| 10. AI gắn với UX nghiệp vụ | AI nằm trong menu, lấy dữ liệu CSDL trực tiếp, có nhật ký và endpoint backend |


## 8.1. Prompt Lab V1/V2/V3

Sau khi cấu hình API key, mở **Trợ lý AI → Thử Prompt V1/V2/V3**. Hệ thống sẽ dùng cùng một snapshot dữ liệu và cùng một câu hỏi để chạy ba phiên bản prompt. Ba kết quả được hiển thị cạnh nhau và tự ghi vào `AiLogs`. Đây là phần minh chứng trực tiếp cho tiêu chí tối ưu prompt qua thử nghiệm.

Nếu chưa cấu hình API key, trang vẫn chạy fallback để kiểm tra luồng nhưng không nên dùng fallback làm bằng chứng đã thử model thật.

## 8.2. Endpoint AI backend

Endpoint tích hợp sâu:

```text
POST /api/ai/summarize-herd
Content-Type: application/json
X-FarmAI-Request: 1
```

Body mẫu:

```json
{
  "task": "summary",
  "question": "Hãy ưu tiên các việc cần xử lý trong 7 ngày tới."
}
```

Endpoint yêu cầu người dùng đã đăng nhập với vai trò **Quản lý** hoặc **Kỹ thuật viên**.

## 9. Cấu trúc thư mục chính

```text
FarmAI_Complete_NET10/
├── FarmAI.sln
├── FarmAI/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   ├── Prompts/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   └── FarmAI.csproj
├── FarmAI.Tests/
├── docs/
└── tests/
```

## 10. Lưu ý AI

**Khuyến cáo: Kết quả do AI tạo ra chỉ mang tính chất tham khảo, không thay thế cho chỉ định của bác sĩ thú y.**

## 11. Bản sửa giao diện 22/09/2026

Bản này đã sửa các lỗi hiển thị được phát hiện khi chạy trên Windows/Chrome với mức scale/zoom lớn:

- Chuyển font toàn hệ thống sang `Times New Roman` để hiển thị tiếng Việt ổn định, không còn khoảng trắng lạ giữa ký tự có dấu.
- Sidebar có vùng cuộn riêng, nền xanh luôn phủ toàn chiều cao màn hình dù menu nhiều mục.
- Header tiếp tục sticky khi cuộn nội dung.
- Chống co/vỡ chữ ở tên người dùng, nút Đăng xuất, tiêu đề và các nút AI.
- Tối ưu responsive cho màn hình desktop 125% Windows scaling / browser zoom.
- Thay logo placeholder bằng logo ICTU đúng theo giao diện mẫu người dùng cung cấp.

## 12. Bản sửa OpenAI bị chậm / trang chờ lâu

Bản này đã sửa luồng AI sau khi cấu hình API key:

- Trang **Trợ lý AI** gọi `POST /api/ai/summarize-herd` bằng `fetch`, không còn reload toàn bộ trang khi chờ OpenAI.
- Có trạng thái **Đang phân tích...**, khóa nút chống bấm lặp và hiển thị thời gian phản hồi.
- Timeout OpenAI giảm còn **18 giây**; nếu quá thời gian hệ thống tự chuyển sang fallback thay vì để giao diện chờ lâu.
- Trình duyệt có chốt an toàn **25 giây** để dừng request UI nếu kết nối localhost/API bất thường.
- Giảm snapshot gửi AI xuống tối đa **8.000 ký tự** và output xuống **700 tokens** để giảm độ trễ/chi phí.
- Gửi `reasoning.effort = none` và `text.verbosity = low` cho tác vụ quản lý ngắn.
- Phân biệt lỗi HTTP 401/403/400/404/429 và đưa nguyên nhân vào kết quả fallback.
- Prompt Lab V1/V2/V3 chạy **song song** phần gọi AI, sau đó ghi SQLite tuần tự để tránh `DbContext` bị truy cập đa luồng.
- Có thêm `FarmAI.slnf` dùng đường dẫn tương đối, có thể di chuyển project sang máy/thư mục khác mà không phụ thuộc thư mục Downloads cũ.

Nếu AI thật gặp lỗi, hệ thống vẫn trả kết quả **Phân tích nội bộ (fallback)** và trang tiếp tục hoạt động. Không lưu API key trong source code.
