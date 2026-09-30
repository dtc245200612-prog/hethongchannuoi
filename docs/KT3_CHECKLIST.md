# Checklist Bài kiểm tra thường xuyên 3

- [x] 1. AI chạy ngay trong hệ thống, phục vụ tóm tắt đàn, nhắc lịch, tăng trưởng và điểm cần theo dõi.
- [x] 2. Kết nối OpenAI Responses API; API key lấy từ User Secrets/biến môi trường, không hardcode.
- [x] 3. System prompt + user template tách khỏi code trong `Prompts/`; output được ràng buộc bằng JSON schema.
- [x] 4. Có V1, V2, V3 và màn hình **Prompt Lab** để chạy cùng dữ liệu, cùng câu hỏi và ghi log ba lần thử.
- [x] 5. AI lấy dữ liệu từ SQLite qua `FarmDataService`; chỉ Manager/Technician có quyền.
- [x] 6. UI hiển thị title, summary, highlights, reminders, warnings và khuyến cáo bắt buộc.
- [x] 7. Có timeout, HTTP 429, mất mạng, response rỗng, JSON lỗi, giới hạn dữ liệu và fallback.
- [x] 8. Có xUnit và manual test cho lịch chăm sóc, vaccine, tăng trưởng, xuất bán, kho, báo cáo và AI.
- [x] 9. Có tài liệu review/refactor/bảo mật bằng AI tại `docs/AI_CODE_REVIEW.md` và nhật ký SDLC.
- [x] 10. AI nằm trong luồng UI chính, đọc dữ liệu nghiệp vụ thật, có nhật ký prompt/response và endpoint backend.

## Minh chứng nên chụp khi nộp

1. Trang đăng nhập và 3 tài khoản demo.
2. Dashboard có số liệu, cảnh báo và biểu đồ.
3. CRUD đàn/chuồng/chăm sóc/vaccine/tăng trưởng/kho/sinh sản/xuất bán.
4. Màn hình báo cáo.
5. Màn hình AI sau khi cấu hình API key, provider hiển thị OpenAI + model.
6. Trang **Thử Prompt V1/V2/V3** sau một lần chạy thật.
7. Nhật ký AI có ba bản ghi `prompt-lab-v1`, `prompt-lab-v2`, `prompt-lab-v3`.
8. Test Explorer xanh.
9. User Secrets hoặc Environment Variables (che API key khi chụp).

## Lưu ý về tiêu chí 4

Project đã có đầy đủ code để chạy 3 vòng thử nghiệm. Để minh chứng là **model thật đã được thử**, bạn cần cấu hình API key và thực hiện một lần Prompt Lab trên máy của mình; project không giả lập hoặc bịa kết quả OpenAI trong tài liệu.
