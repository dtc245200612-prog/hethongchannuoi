# Test cases — FarmAI

## A. Đăng nhập & phân quyền

| ID | Trường hợp | Dữ liệu | Kết quả mong đợi |
|---|---|---|---|
| AUTH-01 | Đăng nhập đúng Quản lý | admin / admin123 | Vào Dashboard, thấy AI/Chi phí/Tài khoản |
| AUTH-02 | Sai mật khẩu | admin / sai | Báo tên đăng nhập hoặc mật khẩu không đúng |
| AUTH-03 | Nhân viên mở `/Ai` | nv / nv123 | Bị chuyển đến AccessDenied |
| AUTH-04 | Kỹ thuật viên mở `/Ai` | ktv / ktv123 | Truy cập được |

## B. Lịch chăm sóc

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| CARE-01 | Ngày dự kiến < hôm nay, chưa hoàn thành | Quá hạn |
| CARE-02 | Ngày dự kiến trong 7 ngày | Sắp đến hạn |
| CARE-03 | Đánh dấu hoàn thành | Lưu CompletedDate và trạng thái Đã hoàn thành |
| CARE-04 | Nhân viên cố tạo lịch chăm sóc | Bị chặn quyền; chỉ Quản lý/Kỹ thuật viên lập lịch |

## C. Vaccine

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| VAC-01 | Ngày dự kiến < hôm nay, chưa hoàn thành | Quá hạn |
| VAC-02 | Ngày dự kiến trong 7 ngày | Sắp đến hạn |
| VAC-03 | Có CompletedDate | Đã tiêm |
| VAC-04 | Nhấn Hoàn thành | CompletedDate = hôm nay, trạng thái Đã tiêm |

## D. Tăng trưởng

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| GROW-01 | Có 3 điểm cân 212.4 → 219.8 → 227.3 | Biểu đồ tăng; rule tính thay đổi 14.9 kg |
| GROW-02 | Chỉ có 1 điểm cân | Không kết luận thay đổi; AI phải nói dữ liệu chưa đủ |

## E. Kho thức ăn

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| FEED-01 | Nhập 50 khi tồn 100 | Tồn mới 150 |
| FEED-02 | Xuất 25 khi tồn 100 | Tồn mới 75 |
| FEED-03 | Xuất 101 khi tồn 100 | Không lưu, báo lỗi |
| FEED-04 | Tồn = mức tối thiểu | Hiển thị cảnh báo Sắp hết |

## F. Xuất bán

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| SALE-01 | Xuất 10 con từ đàn 100 | Giao dịch lưu, đàn còn 90 |
| SALE-02 | Xuất 101 con từ đàn 100 | Không lưu, báo lỗi |
| SALE-03 | Quản lý xóa giao dịch 10 con | Đàn được hoàn lại 10 |

## G. AI

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| AI-01 | Không cấu hình API key | Hệ thống vẫn trả fallback nội bộ, không crash |
| AI-02 | Có API key hợp lệ | Provider hiển thị OpenAI + model |
| AI-03 | API HTTP 429 | Fallback, thông báo nguyên nhân trong summary |
| AI-04 | API timeout | Fallback, không mất dữ liệu CRUD |
| AI-05 | Response rỗng/sai JSON | Fallback |
| AI-06 | Dữ liệu dài | Dữ liệu bị giới hạn trước khi gửi |
| AI-07 | Câu hỏi yêu cầu chẩn đoán | Prompt guardrail yêu cầu không chẩn đoán/kê thuốc |
| AI-08 | Chạy phân tích | Tạo bản ghi trong nhật ký AI |

## H. Báo cáo

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| REP-01 | Có sale + expense | Hiển thị doanh thu, chi phí, chênh lệch |
| REP-02 | Hao hụt = 0, số vật nuôi = 0 | Tỷ lệ hao hụt = 0%, không chia cho 0 |
| REP-03 | Thức ăn dưới ngưỡng | Xuất hiện trong danh sách tồn kho cần chú ý |

## I. Endpoint AI backend

| ID | Trường hợp | Kết quả mong đợi |
|---|---|---|
| API-01 | Manager/Technician POST `/api/ai/summarize-herd` + header `X-FarmAI-Request: 1` | Trả JSON `title/summary/highlights/reminders/warnings` |
| API-02 | Nhân viên gọi endpoint | HTTP 403 |
| API-03 | Thiếu `X-FarmAI-Request` | HTTP 400 |
| API-04 | Task ngoài `summary/schedule/growth/risk` | HTTP 400 |
| API-05 | Không có API key | Trả fallback JSON, ứng dụng không crash |
