# Ghi chú bản tích hợp OpenAI – 13/10/2026

## Những phần đã sửa so với bản chatbot trước

1. `appsettings.json` đặt `AI:ChatProvider=OpenAI`, model `gpt-4.1-mini`, `AI:ReportProvider=OpenAI`.
2. `ChatModelClient.cs`: gọi endpoint OpenAI Chat Completions, Structured Outputs JSON schema strict; giới hạn timeout, retry ngắn, hạn mức, kích thước dữ liệu và kiểm tra source ID.
3. `OpenAiSchemas.cs`: hợp đồng JSON cho chatbot và báo cáo, không cho trường ngoài schema.
4. `OpenAiReportClient.cs`: phân hệ Báo cáo & AI gọi OpenAI với snapshot server, kiểm tra đủ thiếu dữ liệu và dẫn nguồn trước khi lưu nháp.
5. `ChatContextService.cs`: bổ sung số lượng tiêm đã duyệt, phần còn lại, các lịch quá hạn trong mẫu và ADG từ hai mẫu cân có phương pháp trùng nhau. Không tính toàn trang trại từ tập mẫu giới hạn.
6. `ChatSafety.cs`: tăng kiểm tra JSON, từ chối trường lạ và source ID không thuộc quyền.
7. `FarmAI.csproj`: khai báo UserSecretsId để Visual Studio quản lý khóa API bên ngoài mã nguồn.
8. Tài liệu `CAI_OPENAI_TRONG_VISUAL_STUDIO.md` hướng dẫn luân chuyển khóa bị lộ, thêm khóa mới và kiểm tra chatbot/báo cáo.
9. `OpenAiContractsTests.cs`: các kiểm tra hợp đồng JSON và mã nguồn. Không gọi OpenAI thật trong unit test.

## Giới hạn và minh chứng cần thu thập

- Chưa chạy `dotnet build` và `dotnet test` vì môi trường tạo ZIP không có .NET SDK; người dùng phải chạy lại trên Visual Studio.
- Chưa thử kết nối OpenAI thật vì khóa người dùng đã đưa qua chat không được tái sử dụng và không được nhúng vào mã nguồn. Cần thay khóa mới (revoke key cũ).
- Không có benchmark để tuyên bố đáp ứng tiêu chí p95 hoặc cải thiện sau 3 vòng prompt. File benchmark cũ là công cụ kiểm thử, không phải kết quả đã chạy.
- API cần tài khoản Platform có quota và quyền gọi model; ChatGPT Plus không đồng nghĩa quyền truy cập API miễn phí.
- Không triển khai truy cập public/production với dữ liệu demo. Với dữ liệu thật, cần phê duyệt chính sách đưa dữ liệu lên dịch vụ đám mây và bổ sung bảo mật/hạ tầng.

## Các lệnh kiểm tra trên Windows

```powershell
dotnet --info
dotnet restore FarmAI.sln
dotnet build FarmAI.sln -c Debug
dotnet test FarmAI.sln -c Debug
dotnet run --project FarmAI/FarmAI.csproj --launch-profile "FarmAI (Kestrel)"
```
