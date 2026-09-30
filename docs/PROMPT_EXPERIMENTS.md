# Minh chứng tối ưu Prompt V1 → V2 → V3 — FarmAI

Mục tiêu: cùng một dữ liệu trang trại, so sánh ba phiên bản prompt để cải thiện mức độ bám dữ liệu, giới hạn chuyên môn và khả năng trình bày có cấu trúc.

## Cách chạy minh chứng thật

1. Cấu hình `OPENAI_API_KEY` bằng User Secrets hoặc biến môi trường.
2. Đăng nhập bằng `admin/admin123` hoặc `ktv/ktv123`.
3. Mở **Trợ lý AI → Thử Prompt V1/V2/V3**.
4. Giữ cùng một tác vụ và cùng một câu hỏi kiểm thử.
5. Nhấn **Chạy V1, V2 và V3**.
6. Chụp màn hình ba cột kết quả và trang **Nhật ký prompt / phản hồi**.

> Khi chưa có API key, Prompt Lab vẫn chạy bằng fallback nội bộ để kiểm tra luồng, nhưng kết quả fallback không được coi là minh chứng so sánh model thật.

## V1 — Prompt ngắn

File: `FarmAI/Prompts/prompts.v1.json`

- System prompt ngắn, chưa có giới hạn chuyên môn đầy đủ.
- Mục tiêu kiểm tra: xem model có dễ suy diễn ngoài dữ liệu hoặc đưa gợi ý quá rộng hay không.

## V2 — Bổ sung guardrail

File: `FarmAI/Prompts/prompts.v2.json`

- Chỉ dùng dữ liệu cung cấp.
- Không tự bịa dữ liệu.
- Không chẩn đoán bệnh.
- Tách ý thành tóm tắt / nhắc việc / cảnh báo.

Mục tiêu kiểm tra: so sánh với V1 về mức độ bám dữ liệu và an toàn chuyên môn.

## V3 — Prompt hiện tại

File: `FarmAI/Prompts/prompts.json`

- System prompt đầy đủ guardrail.
- 4 tác vụ: `summary`, `schedule`, `growth`, `risk`.
- Yêu cầu nêu rõ khi dữ liệu chưa đủ.
- Output được ràng buộc bằng JSON schema ở `OpenAiService` với các trường:
  `title`, `summary`, `highlights`, `reminders`, `warnings`.
- Không suy đoán nguyên nhân y khoa và không kê thuốc.

Mục tiêu kiểm tra: đầu ra ổn định, dễ parse, ngắn gọn và gắn trực tiếp với nghiệp vụ.

## Bảng ghi kết quả khi chạy thật

| Lần | Prompt | Model/provider | Điểm quan sát | Kết luận |
|---|---|---|---|---|
| 1 | V1 | Ghi từ giao diện | Điền sau khi chạy thật |  |
| 2 | V2 | Ghi từ giao diện | Điền sau khi chạy thật |  |
| 3 | V3 | Ghi từ giao diện | Điền sau khi chạy thật |  |

Prompt Lab tự ghi ba lần chạy vào bảng `AiLogs`, vì vậy ngoài ảnh chụp màn hình bạn còn có lịch sử trong CSDL để chứng minh quá trình thử nghiệm.
