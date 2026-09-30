# Minh chứng review code và cải thiện chất lượng bằng AI

Tài liệu này ghi lại các điểm đã được rà soát trong quá trình hoàn thiện FarmAI.

## 1. Bảo mật API key

**Phát hiện:** hardcode API key trong source/appsettings sẽ dễ bị lộ khi đẩy Git.

**Cải thiện:** `OpenAiService` đọc key từ `IConfiguration` / biến môi trường. Project hỗ trợ `dotnet user-secrets`. `.gitignore` loại `.env`, database và file secrets.

## 2. Phân quyền AI

**Phát hiện:** nếu mọi tài khoản đều gọi AI, dữ liệu vận hành có thể bị gửi ra ngoài không đúng quyền.

**Cải thiện:** `AiController` có `[Authorize(Roles = Manager,Technician)]`. Nhân viên không thấy menu AI và bị chặn ở backend nếu truy cập URL trực tiếp.

## 3. CSRF cho thao tác thay đổi dữ liệu

**Phát hiện:** các POST CRUD cần chống request giả mạo.

**Cải thiện:** `Program.cs` thêm `AutoValidateAntiforgeryTokenAttribute`; form tag helper của MVC sinh antiforgery token.

## 4. Mật khẩu

**Phát hiện:** demo LocalStorage trước đây lưu mật khẩu dạng text.

**Cải thiện:** bản MVC dùng `PasswordHasher<AppUser>` và cookie authentication. Database chỉ lưu `PasswordHash`.

## 5. Kiểm soát tồn kho

**Phát hiện:** xuất thức ăn nhiều hơn tồn kho có thể làm số lượng âm.

**Cải thiện:** `FarmRules.ApplyStockTransaction` chặn xuất quá tồn và được kiểm thử tự động.

## 6. Xuất bán

**Phát hiện:** ghi xuất bán nhưng không cập nhật số lượng đàn làm dữ liệu không đồng nhất.

**Cải thiện:** `SalesController.Create` tự trừ số lượng đàn; khi Quản lý xóa giao dịch, số lượng được hoàn lại.

## 7. Lỗi AI

**Phát hiện:** API có thể timeout, 429, mất mạng, trả response rỗng hoặc sai JSON.

**Cải thiện:** `OpenAiService` có timeout, xử lý HTTP 429, `HttpRequestException`, `TaskCanceledException`, JSON sai định dạng và fallback nội bộ.

## 8. Dữ liệu AI quá dài

**Phát hiện:** gửi toàn bộ lịch sử có thể vượt context hoặc tăng chi phí.

**Cải thiện:** `FarmDataService` chỉ lấy dữ liệu nghiệp vụ gần đây phù hợp; `OpenAiService` giới hạn số ký tự trước khi gửi.

## 9. Guardrail chuyên môn

**Phát hiện:** AI có thể biến nhật ký sức khỏe thành chẩn đoán.

**Cải thiện:** prompt yêu cầu không chẩn đoán/kê thuốc; UI hiển thị cảnh báo bắt buộc; kho thuốc chỉ là quản lý tồn/hạn dùng.

## 10. Tính kiểm thử

**Cải thiện:** tách quy tắc nghiệp vụ vào `FarmRules` để có thể test độc lập. `FarmAI.Tests` bao gồm đúng/sai/biên cho vaccine, tồn kho, hao hụt.
