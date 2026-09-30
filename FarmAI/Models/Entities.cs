using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAI.Models;

public static class RoleNames
{
    public const string Manager = "Manager";
    public const string Technician = "Technician";
    public const string Employee = "Employee";

    public static string ToVietnamese(string role) => role switch
    {
        Manager => "Quản lý",
        Technician => "Kỹ thuật viên",
        _ => "Nhân viên"
    };
}

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = RoleNames.Employee;

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class Barn
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Mã chuồng")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Tên chuồng")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100000)]
    [Display(Name = "Sức chứa")]
    public int Capacity { get; set; }

    [Range(-20, 70)]
    [Display(Name = "Nhiệt độ (°C)")]
    public decimal Temperature { get; set; }

    [Range(0, 100)]
    [Display(Name = "Độ ẩm (%)")]
    public int Humidity { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Tốt";

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public ICollection<Herd> Herds { get; set; } = new List<Herd>();
}

public class Herd
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Mã đàn")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Tên đàn")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(60)]
    [Display(Name = "Loài vật nuôi")]
    public string Species { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Giống")]
    public string? Breed { get; set; }

    [Display(Name = "Chuồng")]
    public int BarnId { get; set; }
    public Barn? Barn { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required, StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Ổn định";

    [Range(0, 1000000)]
    [Display(Name = "Số hao hụt")]
    public int MortalityCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public ICollection<CareLog> CareLogs { get; set; } = new List<CareLog>();
    public ICollection<CareSchedule> CareSchedules { get; set; } = new List<CareSchedule>();
    public ICollection<VaccineRecord> Vaccines { get; set; } = new List<VaccineRecord>();
    public ICollection<GrowthRecord> GrowthRecords { get; set; } = new List<GrowthRecord>();
    public ICollection<ReproductionRecord> ReproductionRecords { get; set; } = new List<ReproductionRecord>();
    public ICollection<SaleRecord> Sales { get; set; } = new List<SaleRecord>();
}

public class CareLog
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, StringLength(80)]
    [Display(Name = "Loại công việc")]
    public string Type { get; set; } = "Chăm sóc";

    [Required, StringLength(800)]
    [Display(Name = "Nội dung")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Người thực hiện")]
    public string StaffName { get; set; } = string.Empty;

    [Display(Name = "Cần kiểm tra sức khỏe")]
    public bool RequiresHealthCheck { get; set; }
}

public class CareSchedule
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [Required, StringLength(160)]
    [Display(Name = "Công việc chăm sóc")]
    public string Title { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày dự kiến")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hoàn thành")]
    public DateTime? CompletedDate { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Đã lên lịch";

    [StringLength(100)]
    [Display(Name = "Người phụ trách")]
    public string? Assignee { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class VaccineRecord
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Vaccine")]
    public string VaccineName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày dự kiến")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày thực hiện")]
    public DateTime? CompletedDate { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Chưa tiêm";

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class GrowthRecord
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày ghi nhận")]
    public DateTime RecordedDate { get; set; } = DateTime.Today;

    [Range(0.01, 5000)]
    [Display(Name = "Khối lượng TB (kg/con)")]
    public decimal AverageWeight { get; set; }

    [Range(1, 1000000)]
    [Display(Name = "Số con cân")]
    public int SampleSize { get; set; } = 1;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class FeedItem
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Tên thức ăn")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Đơn vị")]
    public string Unit { get; set; } = "kg";

    [Range(0, 100000000)]
    [Display(Name = "Tồn kho")]
    public decimal Quantity { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Mức tối thiểu")]
    public decimal MinimumStock { get; set; }

    [Range(0, 1000000000)]
    [Display(Name = "Đơn giá")]
    public decimal UnitCost { get; set; }

    public ICollection<FeedTransaction> Transactions { get; set; } = new List<FeedTransaction>();
}

public class FeedTransaction
{
    public int Id { get; set; }

    [Display(Name = "Thức ăn")]
    public int FeedItemId { get; set; }
    public FeedItem? FeedItem { get; set; }

    [Display(Name = "Đàn")]
    public int? HerdId { get; set; }
    public Herd? Herd { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, StringLength(20)]
    [Display(Name = "Loại")]
    public string TransactionType { get; set; } = "Xuất";

    [Range(0.01, 100000000)]
    [Display(Name = "Số lượng")]
    public decimal Quantity { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class MedicineItem
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Tên thuốc / vật tư")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Đơn vị")]
    public string Unit { get; set; } = "lọ";

    [Range(0, 1000000)]
    [Display(Name = "Tồn kho")]
    public decimal Quantity { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Mức tối thiểu")]
    public decimal MinimumStock { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hạn sử dụng")]
    public DateTime? ExpiryDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class ReproductionRecord
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [Required, StringLength(60)]
    [Display(Name = "Sự kiện")]
    public string EventType { get; set; } = "Phối giống";

    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(200)]
    [Display(Name = "Kết quả")]
    public string? Result { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày dự kiến tiếp theo")]
    public DateTime? NextExpectedDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class SaleRecord
{
    public int Id { get; set; }

    [Display(Name = "Đàn")]
    public int HerdId { get; set; }
    public Herd? Herd { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày xuất bán")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Range(1, 1000000)]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }

    [Range(0.01, 5000)]
    [Display(Name = "Khối lượng TB (kg)")]
    public decimal AverageWeight { get; set; }

    [Range(0.01, 1000000000)]
    [Display(Name = "Đơn giá (đ/kg)")]
    public decimal UnitPrice { get; set; }

    [StringLength(160)]
    [Display(Name = "Khách hàng")]
    public string? Buyer { get; set; }

    [NotMapped]
    [Display(Name = "Doanh thu")]
    public decimal TotalAmount => Quantity * AverageWeight * UnitPrice;
}

public class ExpenseRecord
{
    public int Id { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, StringLength(80)]
    [Display(Name = "Nhóm chi phí")]
    public string Category { get; set; } = "Thức ăn";

    [Required, StringLength(250)]
    [Display(Name = "Nội dung")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000000)]
    [Display(Name = "Số tiền")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}

public class AiLog
{
    public int Id { get; set; }
    public int? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    [StringLength(50)]
    public string Task { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Question { get; set; } = string.Empty;

    public string Response { get; set; } = string.Empty;

    [StringLength(80)]
    public string Provider { get; set; } = string.Empty;

    public bool Success { get; set; }

    [StringLength(1000)]
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
