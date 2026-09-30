# Kiến trúc FarmAI

## Công nghệ

- ASP.NET Core MVC .NET 10
- Entity Framework Core 10 + SQLite
- Cookie Authentication + Role Authorization
- Kestrel
- OpenAI Responses API qua `HttpClient`
- Razor Views + CSS/JavaScript thuần
- xUnit cho test tự động

## Luồng chính

```text
Browser
  ↓ HTTPS/Kestrel
Controllers
  ├─ Authorization / validation
  ├─ Entity Framework Core → SQLite
  ├─ AiController / Prompt Lab
  └─ AiApiController (`POST /api/ai/summarize-herd`)
       ↓
     FarmDataService → dữ liệu nghiệp vụ có giới hạn 14 ngày / lịch 7 ngày
       ↓
     PromptRepository → V1 / V2 / V3
       ↓
     OpenAiService → OpenAI Responses API + JSON schema
       └─ lỗi/không key → fallback nội bộ
```

## Quan hệ dữ liệu

```text
Barn 1 ─── n Herd
Herd 1 ─── n CareLog
Herd 1 ─── n VaccineRecord
Herd 1 ─── n GrowthRecord
Herd 1 ─── n ReproductionRecord
Herd 1 ─── n SaleRecord
FeedItem 1 ─── n FeedTransaction
Herd 0..1 ─── n FeedTransaction
AppUser 0..1 ─── n AiLog
MedicineItem (quản lý kho độc lập)
ExpenseRecord (chi phí vận hành)
```

## Quy tắc phân quyền

| Chức năng | Quản lý | Kỹ thuật viên | Nhân viên |
|---|:---:|:---:|:---:|
| Xem dashboard/dữ liệu | ✓ | ✓ | ✓ |
| CRUD đàn/chuồng | ✓ | ✓ | xem |
| Ghi nhật ký chăm sóc | ✓ | ✓ | ✓ |
| Vaccine/tăng trưởng/sinh sản | ✓ | ✓ | xem |
| Nhập/xuất thức ăn | ✓ | ✓ | ✓ |
| Sửa kho thuốc/vật tư | ✓ | ✓ | xem |
| Xuất bán | ✓ | ✓ | xem |
| Chi phí | ✓ | — | — |
| Báo cáo | ✓ | ✓ | — |
| Trợ lý AI | ✓ | ✓ | — |
| Quản lý tài khoản | ✓ | — | — |
