# Kịch bản kiểm thử/đề xuất quay demo

Chỉ là **test plan**, chưa phải báo cáo kiểm thử đã chạy. Cần lưu màn hình, log, phiên bản mã, cấu hình SQL Server khi thực hiện thật.

1. **Tạo chuồng và đàn:** quản lý tạo chuồng sức chứa 120, nhập đàn 100 → 1 herd, 1 RECEIPT, 1 allocation; nhập tiếp 30 tại chuồng còn 20 chỗ → từ chối và không tạo herd.
2. **Chuyển chuồng:** đưa đàn sang chuồng đủ chỗ → quantity không đổi, phân bổ cũ đóng và mới mở; dùng version cũ → từ chối.
3. **Chia đàn:** nguồn 100, đích 20, chia 25 → nguồn 75, đích 45, hai movement có cùng SourceId; chia 1000 → không thay đổi gì.
4. **Hao hụt:** nhân viên báo giảm 2 → quantity chưa đổi; quản lý duyệt → giảm đúng 2, có audit.
5. **Nhật ký và quyền:** nhân viên ghi FEED thiếu giá → Cost null/PENDING; cùng nhân viên không tự duyệt; kỹ thuật viên trong phạm vi duyệt → APPROVED; ngoài chuồng → bị chặn.
6. **Cân:** 10 con 200kg ngày A, 10 con 270kg sau 14 ngày → ADG 0,5 kg/ngày; đổi phương pháp hoặc trùng thời điểm → null.
7. **Sinh sản:** mẹ FEMALE, sống 10, lưu 1, tiếp nhận 8 → tăng 8; gửi lại cùng RequestKey → không tăng thêm.
8. **Tiêm:** target=100; báo 60 và duyệt; báo 25 và duyệt → remaining=15, PARTIAL. Báo thêm 20 và duyệt → từ chối.
9. **Cảnh báo:** chạy Refresh hai lần cùng ngày → không nhân bản notification cùng khóa.
10. **Bán:** phiếu DRAFT gồm 20 con → đàn không đổi; xác nhận → giảm 20 và movement/audit; xác nhận lại khóa cũ → không giảm thêm; phiếu có hai dòng cộng vượt tồn → toàn giao dịch bị từ chối.
11. **Thanh toán:** tổng phiếu 60 triệu, thu 40 triệu → dư 20 triệu; cố thu thêm 21 triệu → từ chối.
12. **AI:** không cấu hình model → RULE_BASED được gắn nhãn chính xác; khi Ollama cấu hình đủ → draft OLLAMA có source_id; nội dung sai JSON/sai source → từ chối, không viết số lượng đàn; người có quyền duyệt → APPROVED.
13. **Bảo mật:** đăng xuất và dùng lại cookie; nhân viên thử URL Sales/Admin; nhân viên của C01 thử Herd chi tiết thuộc C02; đổi role xong session cũ bị thu hồi.
14. **Đồng thời:** trên SQL Server dùng hai yêu cầu xác nhận bán chung herd, tổng vượt tồn → chỉ giao dịch hợp lệ được commit. Nên triển khai k6/JMeter/Playwright khi nghiệm thu.

**Chạy xUnit:** `dotnet test FarmAI.sln`. Chưa có E2E browser tự động hoặc kết quả benchmark p95, AI 30 ca, RTO/RPO trong gói.
