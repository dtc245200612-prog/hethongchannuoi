# FARM AI – HỆ THỐNG QUẢN LÝ CHĂN NUÔI TÍCH HỢP AI

**Mã nguồn học phần / bản demo có giao diện và cơ sở dữ liệu thật**, chuyển thể từ tài liệu “Báo cáo kết thúc học phần” (78 trang, 12/10/2026). Kiến trúc đã được đổi từ **FastAPI + React + PostgreSQL** trong báo cáo sang **ASP.NET Core MVC + Razor + EF Core + SQL Server** theo yêu cầu. Vì vậy đây là hiện thực hóa C# theo nghiệp vụ của báo cáo, **không phải mã Python/React gốc**.

## Mở trong Visual Studio Community 2026 Insiders

1. Giải nén ZIP vào một thư mục dễ truy cập, ví dụ `D:\FarmAI`.
2. Trong Visual Studio, cài workload **ASP.NET and web development** và **.NET 10 SDK**. Cần SQL Server Express **LocalDB** (thường có thể cài cùng Visual Studio) hoặc SQL Server 2022+.
3. Chọn **File → Open → Project/Solution** rồi mở `FarmAI.sln` ở thư mục gốc.
4. Chọn dự án **FarmAI** làm **Startup Project** (không chọn FarmAI.Tests). Chọn profile **FarmAI (Kestrel)**; không dùng IIS Express.
5. Bấm **Ctrl+F5** hoặc **F5**. Ở chế độ Development, EF Core tự tạo CSDL `FarmAI_DB` nếu chưa có, sau đó seed 3 tài khoản, 2 chuồng, 2 đàn, mẫu cân và 2 tên vaccine giả định.
6. Truy cập **https://localhost:7105** (hoặc **http://localhost:5105**). Nếu HTTPS dev cert chưa được tin cậy, dùng `dotnet dev-certs https --trust` trong Terminal và chạy lại.

### Tài khoản mẫu (chỉ cho môi trường Development)

| Username | Vai trò | Mật khẩu mặc định |
|---|---|---|
| `quanly` | Quản lý | `FarmAI@2026` |
| `kythuat` | Kỹ thuật viên | `FarmAI@2026` |
| `nhanvien` | Nhân viên | `FarmAI@2026` |

**Quan trọng:** đây là mật khẩu công khai để trình diễn trên máy cục bộ, không dùng khi đưa hệ thống lên Internet. Đổi bằng biến môi trường `FarmAI__SeedPassword` **trước lần tạo CSDL đầu tiên**. Các tài khoản cũ không tự đổi mật khẩu khi cấu hình thay đổi; không dùng dữ liệu demo thật.

### Chạy với SQL Server khác LocalDB

Nếu máy không có LocalDB, mở PowerShell trong thư mục mã nguồn và dùng SQL Server 2022 (cài sẵn hoặc Docker):

```powershell
# Tùy chọn: tạo SQL Server trên Docker (phải tạo file .env từ .env.example và thay mật khẩu)
docker compose -f docker-compose.sqlserver.yml up -d

# Đặt connection string an toàn theo cấu hình SQL Server của bạn
$env:ConnectionStrings__SqlServer = 'Server=localhost,1433;Database=FarmAI_DB;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet restore FarmAI.sln
dotnet run --project FarmAI/FarmAI.csproj --launch-profile 'FarmAI (Kestrel)'
```

### Chạy nhanh bằng SQLite (tùy chọn để demo, **không phải DB chính**)

```powershell
$env:FarmAI__DbProvider = 'Sqlite'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project FarmAI/FarmAI.csproj --launch-profile 'FarmAI (Kestrel)'
```

SQL Server là lựa chọn mặc định vì yêu cầu đầu bài chưa chỉ định rõ một lựa chọn trong SQL Server/MySQL/không dùng DB. Bản này **không có adapter MySQL**. Khi dùng SQLite, dữ liệu nằm ở `farm-ai.db` (không đưa vào ZIP).

## Sử dụng từng chức năng

- **Trang chủ:** tổng số đàn, số con trong phạm vi, hao hụt ghi nhận, lịch tiêm sắp/quá hạn.
- **Chuồng:** quản lý tạo chuồng và sửa sức chứa/trạng thái. Không ngừng chuồng đang có con.
- **Đàn:** tạo hồ sơ + biến động nhập + phân bổ chuồng đồng thời; tra cứu/lọc/phân trang; chuyển toàn đàn; chia sang đàn có sẵn; báo hao hụt/điều chỉnh. Nhân viên gửi biến động để quản lý xác nhận. Chi tiết hiển thị ID đàn đích phục vụ chia đàn.
- **Nhật ký chăm sóc:** thức ăn, thuốc, theo dõi; giá trống giữ null; bản ghi chờ xác nhận; người nhập không tự duyệt; chi phí duyệt tính bằng decimal.
- **Cân đo:** số mẫu, tổng kg, phương pháp; bình quân và ADG; khi thiếu phương pháp tương thích không tính ADG.
- **Sinh sản:** mã tai, cá thể mẹ; sinh sống, chết lưu, số tiếp nhận; tăng đàn kèm biến động trong cùng transaction; khóa chống gửi lại.
- **Lịch tiêm:** tạo kế hoạch, báo lần thực hiện và kỹ thuật viên xác nhận; chưa duyệt không tính tổng; dời/hủy có lý do, giữ lịch sử audit; worker cập nhật nhắc việc mỗi giờ (và nút cập nhật thủ công).
- **Xuất bán:** phiếu nhiều dòng nháp, xác nhận và giảm đàn một lần, thanh toán từng phần; doanh số tách khỏi tiền thực thu; chỉ quản lý được truy cập.
- **Trợ lý chatbot AI:** giao diện chat nổi và trang riêng; trả lời theo dữ liệu đúng quyền; Ollama/OpenAI, nguồn và cảnh báo; không sửa DB.
- **Báo cáo + AI:** số liệu theo kỳ, ảnh chụp dữ liệu nguồn (snapshot SHA256), bản dự thảo, các source ID, cảnh báo thú y, phê duyệt và audit. AI không có quyền tự sửa CSDL.
- **Tài khoản:** tạo user, đổi vai trò/kích hoạt, phân công chuồng, audit. Không thể khóa/hạ quyền quản lý cuối cùng.

### Bật Ollama cục bộ (AI thật)

1. Cài Ollama từ nguồn chính thức và khởi chạy dịch vụ trên `http://localhost:11434`.
2. Tải một mô hình phù hợp, ví dụ `ollama pull llama3.2:3b` (tên model có thể thay đổi nếu máy chưa có mô hình này).
3. Trước khi mở ứng dụng, thiết lập biến môi trường trên PowerShell:

```powershell
$env:AI__BaseUrl = 'http://localhost:11434'
$env:AI__Model = 'llama3.2:3b'
# Tùy chọn: API gateway có xác thực sẽ cần bổ sung header qua cấu hình riêng.
```

4. Mở **Báo cáo & AI → Tạo bản dự thảo**. Nếu model trả đúng schema và source ID, kết quả lưu dạng `OLLAMA` chờ người có thẩm quyền kiểm tra. Nếu **không cấu hình** 2 biến môi trường trên, hệ thống dùng `RULE_BASED` – một bản nhận xét theo quy tắc, **không quảng bá là kết quả từ mô hình AI**. Khi AI lỗi, dữ liệu sản xuất vẫn giữ nguyên.

## Chatbot AI trực tiếp (đã bổ sung)

Sau khi đăng nhập, chọn **Trợ lý chatbot AI** ở menu bên trái hoặc bấm **✦ Trợ lý AI** tại góc dưới bên phải trên các trang quản lý. Có câu hỏi gợi ý, gửi Enter, Shift+Enter xuống dòng, xóa hội thoại, hiển thị nguồn dữ liệu, thông báo lỗi và cảnh báo thú y. Không yêu cầu thay đổi CSDL; hỏi đáp **chỉ đọc**.

**Lưu ý:** Mặc định khi chưa cấu hình model, chatbot vẫn trả lời một số câu hỏi cơ bản về dữ liệu bằng **RULE_BASED**, không giả làm mô hình AI. Muốn chatbot trả lời linh hoạt phải bật một trong hai provider:

### Cách 1 — Ollama trên máy (miễn phí dùng local model)

Mở Ollama, tải model và đặt biến môi trường **trong cửa sổ PowerShell trước khi khởi chạy dự án**:

```powershell
ollama pull llama3.2:3b
$env:AI__ChatProvider = 'Ollama'
$env:AI__ChatModel = 'llama3.2:3b'
$env:AI__BaseUrl = 'http://localhost:11434'
dotnet run --project FarmAI/FarmAI.csproj --launch-profile 'FarmAI (Kestrel)'
```

`AI__ChatModel` để trống thì chatbot thử dùng `AI__Model` của phân hệ Báo cáo AI cũ. Model và Ollama phải chạy sẵn; model lớn có thể cần RAM nhiều hơn.

### Cách 2 — OpenAI qua API

Không dán khóa vào file JS, HTML, `appsettings.json` hoặc GitHub. Đặt biến môi trường riêng (PowerShell) **trên máy chủ**:

```powershell
$env:AI__ChatProvider = 'OpenAI'
$env:AI__ChatModel = 'gpt-4o-mini'
$env:OPENAI_API_KEY = 'PASTE_YOUR_KEY_HERE'
dotnet run --project FarmAI/FarmAI.csproj --launch-profile 'FarmAI (Kestrel)'
```

Mẫu `PASTE_YOUR_KEY_HERE` phải thay bằng API key riêng, tuyệt đối không gửi key trong chat hoặc chụp màn hình. OpenAI API có thể phát sinh chi phí theo tài khoản API; gói ChatGPT Plus không tự bao gồm chi phí API. Trên Visual Studio, thiết lập biến môi trường trong **Project Debug settings** hoặc `dotnet user-secrets` (không cam kết ứng dụng đang chạy sẽ tự đọc biến vừa đặt trong cửa sổ terminal khác). **Khởi động lại ứng dụng** sau khi thay cấu hình.

**Kiểm tra:** vào `/Chat` sau đăng nhập; bên phải hiển thị tình trạng provider. Trả lời thật hiển thị nhãn `OLLAMA`/`OPENAI`; nếu `RULE_BASED` nghĩa là chưa cấu hình model. Nếu gặp lỗi 429, 502, 503, 504 hãy xem thông báo và kiểm tra model/cấu hình, không cần xóa dữ liệu quản lý.

- `Services/ChatContextService.cs`: lấy dữ liệu đàn, lịch tiêm, cân đo, nhật ký **đã lọc theo phân công**.
- `Services/ChatModelClient.cs`: kết nối AI, giới hạn 30s, kiểm JSON và source ID, giới hạn kích cỡ/nhịp gọi.
- `Prompts/chat_system_v1/v2/v3.txt`: ba phiên bản prompt phục vụ đối chiếu, v3 được dùng khi chạy app.
- `scripts/evaluate_chat_prompts.py`: tự chạy 3 vòng x 6 ca trên Ollama cục bộ và xuất bảng kết quả thực nghiệm.
- `docs/CHATBOT_AI_TIEU_CHI_TX3.md`, `CHATBOT_KIEM_THU.md`, `CHATBOT_REVIEW_AI.md`: đối chiếu đủ 10 tiêu chí, ca test và nhật ký review.

**Quyền dữ liệu:** query chỉ lấy những đàn thuộc chuồng được phân công, tối đa 25 hồ sơ đàn và các bản ghi gần nhất. Tài khoản nhân viên không được đưa dữ liệu bán hàng/khách hàng sang AI. Chat không có công cụ sửa dữ liệu, không lưu hội thoại vào DB. **Chỉ gửi dữ liệu trang trại lên OpenAI khi được phép sử dụng dịch vụ đám mây theo chính sách đơn vị.**

## Cấu trúc mã nguồn

```
FarmAI.sln                       giải pháp mở bằng Visual Studio
FarmAI/
  FarmAI.csproj                  dự án ASP.NET Core MVC net10.0
  Program.cs                     DI, Kestrel, RBAC, cookies, rate limiting
  Controllers/                   điều hướng, xác thực, thao tác HTML
  Models/                        thực thể và mẫu nhập liệu, FarmRules
  Data/                          EF Core context, seed dữ liệu giả định
  Services/                      dịch vụ nghiệp vụ, quyền, cảnh báo, AI
  Views/                         Razor templates theo chức năng
  Prompts/                       Chatbot system/user prompt V1–V3
  wwwroot/css/                   CSS responsive 360px+
  wwwroot/js/                    Trình chat JavaScript an toàn
  Properties/launchSettings.json  profile Kestrel
FarmAI.Tests/                     kiểm thử quy tắc tính toán (xUnit)
docs/                            đối chiếu Use Case và giới hạn
```

## Kiểm thử

Trong Visual Studio: **Test → Run All Tests**. Hoặc:

```powershell
dotnet restore FarmAI.sln
dotnet build FarmAI.sln -c Debug
dotnet test FarmAI.sln -c Debug
```

Các xUnit test kiểm tra bảo toàn số lượng, âm tồn, tiền/giá, ADG, thay đổi phương pháp, chia cho 0 và các ràng buộc đầu vào/đầu ra chatbot. Kịch bản thao tác tay theo `docs/KIEM_THU.md`. **Tác giả gói nguồn chưa chạy được dotnet build/test ở môi trường tạo gói vì môi trường đó không cài .NET SDK và không truy cập được NuGet; cần chạy ba lệnh trên trên máy bạn trước khi xác nhận đã pass.**

## Giới hạn và lưu ý bảo mật

Đây là **bản demo học thuật**, không phải bản production-ready. Không đưa trực tiếp lên Internet hoặc dùng vào quyết định điều trị. Một số yêu cầu từ báo cáo vẫn là **bổ sung khi sản xuất**:

- Bản ASP.NET MVC dùng cookie có thời hạn 15 phút và session DB, thay vì JWT access + refresh token xoay vòng và hash Argon2id như báo cáo. PasswordHasher ASP.NET Core hiện dùng PBKDF2. Chưa có màn hình đổi mật khẩu, refresh token hoặc một API JWT đầy đủ.
- DB được khởi tạo bằng `EnsureCreated` **chỉ trong Development**; trước production phải tạo và review **EF Core migrations** (`dotnet ef migrations add InitialCreate --project FarmAI --startup-project FarmAI`) và chạy migrate sau sao lưu. App chủ động từ chối chạy ở Production để tránh tạo CSDL nguy hiểm.
- Giao diện demo không hỗ trợ tạo đàn đích mới ngay trong thao tác chia đàn, phân trang dài ở mọi module, sửa nhật ký đã duyệt bằng chứng từ điều chỉnh, kiểm tra thời gian ngừng thuốc và đăng ký vaccine chuyên môn.
- Cảnh báo chạy worker trong tiến trình ứng dụng, không phải cron phân tán. AI dùng một lần gọi giới hạn ~25 giây, chưa có retry phân tán, schema nghiêm ngặt đầy đủ hoặc bộ benchmark 30 ca/ba phiên bản prompt. Kết quả là bản nháp có người kiểm tra.
- Dashboard chỉ tổng hợp dòng còn trong phạm vi hiện hành; so sánh lịch sử và tỷ lệ hao hụt toàn trang trại cần đối soát tồn đầu kỳ chính xác trong môi trường thực. Không tuyên bố đạt p95, RPO, RTO hay uptime khi chưa đo.
- Cần thêm HTTPS hợp lệ, rate limit phân tán, vault quản lý bí mật, CSRF/Origin theo topology thật, giám sát, backup ngoài máy, kiểm thử đồng thời trên SQL Server, chứng từ kế toán và chính sách bảo vệ dữ liệu trước khi triển khai thực.

**Nguồn nghiệp vụ:** tệp báo cáo người dùng cung cấp, phần 1.4, 1.6, bảng 30 UC trang 21–30, ERD và từ điển dữ liệu trang 41–44, AI trang 58–64, kiểm thử trang 65–69. Xem `docs/DOI_CHIEU_UC.md` để phân biệt nội dung đã hiện thực và các khoảng trống.

## Phiên bản nâng cấp OpenAI (13/10/2026)

**Xem `docs/CAI_OPENAI_TRONG_VISUAL_STUDIO.md` để bật OpenAI trong Visual Studio.**

- Mặc định ChatProvider / ReportProvider = `OpenAI`, ChatModel / ReportModel = `gpt-4.1-mini` (có thể tùy chỉnh); nếu chưa có khóa mới, chatbot và báo cáo vẫn dùng `RULE_BASED` minh bạch, không giả làm AI.
- Người dùng phải **thu hồi khóa API đã chia sẻ công khai và tạo khóa mới**, đặt khóa mới trong **Manage User Secrets**. Không có khóa thật hoặc thông tin thanh toán trong ZIP.
- Chatbot dùng OpenAI Chat Completions API với **Structured Outputs `json_schema` strict**, giới hạn 30 giây, xác minh source_ids ở backend, chống hiển thị HTML từ AI và hạn mức hỏi đáp.
- Snapshot chatbot mở rộng dữ liệu theo quyền: số lượng tiêm đã được xác nhận, số còn lại, lịch quá hạn trong mẫu truy vấn và so sánh ADG giữa các mẫu cân đủ điều kiện. Các tổng vượt phạm vi lấy mẫu phải có cảnh báo giới hạn.
- **Báo cáo & AI** có đường kết nối OpenAI riêng với JSON schema, ràng buộc nguồn và dữ liệu thiếu; báo cáo vẫn ở trạng thái DRAFT và cần phê duyệt của người được phân quyền.
- Không có khả năng dùng API key mới nếu chỉ mở ZIP mà không cài .NET 10, cấu hình SQL Server, nhập khóa và có quota OpenAI Platform. Bản này chưa được chạy dotnet build/test trong container vì không có .NET SDK. Xin chạy các lệnh ở phần **Kiểm thử** trên máy Windows.


## Cải thiện giao diện: menu đứng yên khi cuộn trang
- Header FarmAI luôn nằm phía trên; menu bên trái và nội dung nằm ở hai vùng tách biệt.
- Khi cuộn chuột trong nội dung bên phải, chỉ phần nội dung di chuyển; menu bên trái giữ nguyên vị trí.
- Nếu menu dài hơn chiều cao cửa sổ, có thể cuộn menu riêng khi trỏ chuột vào menu.
- Khi thay đổi CSS mà trình duyệt chưa cập nhật, nhấn `Ctrl + F5` để tải lại tài nguyên.
- File chỉnh: `FarmAI/wwwroot/css/site.css` (đoạn **CỐ ĐỊNH THANH CHỨC NĂNG BÊN TRÁI**).

## Giao diện mới Emerald Harvest (13/10/2026)

Thiết kế FarmAI được làm mới theo cảm hứng **trang trại xanh, công nghệ thông minh**: banner trang trại SVG, dashboard KPI, menu kèm icon và đánh dấu màn hình đang chọn, thẻ AI, bảng/biểu mẫu hiện đại, giao diện responsive. **Giữ nguyên chức năng quản lý và tích hợp OpenAI**. Thanh bên và nội dung cuộn độc lập trên desktop; trên mobile dùng nút menu. Xem `docs/GIAO_DIEN_FARMAI_2026.md` để biết những file đã thay và cách nâng cấp dự án cũ mà không mất cấu hình API key.
