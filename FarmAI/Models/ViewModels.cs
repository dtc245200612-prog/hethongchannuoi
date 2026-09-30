using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FarmAI.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = "admin";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "admin123";

    public string? ReturnUrl { get; set; }
}

public class DashboardViewModel
{
    public int HerdCount { get; set; }
    public int AnimalCount { get; set; }
    public int OverdueVaccineCount { get; set; }
    public decimal Revenue { get; set; }
    public int LowStockCount { get; set; }
    public int HealthCheckCount { get; set; }
    public List<VaccineRecord> Vaccines { get; set; } = [];
    public List<CareLog> RecentCare { get; set; } = [];
    public List<GrowthRecord> Growth { get; set; } = [];
}

public class HerdFormViewModel
{
    public Herd Herd { get; set; } = new();
    public List<SelectListItem> Barns { get; set; } = [];
}

public class GenericHerdFormViewModel<T> where T : new()
{
    public T Item { get; set; } = new();
    public List<SelectListItem> Herds { get; set; } = [];
}

public class CareIndexViewModel
{
    public List<CareLog> Logs { get; set; } = [];
    public List<CareSchedule> Schedules { get; set; } = [];
}

public class FeedIndexViewModel
{
    public List<FeedItem> Items { get; set; } = [];
    public List<FeedTransaction> Transactions { get; set; } = [];
    public List<MedicineItem> Medicines { get; set; } = [];
}

public class FeedTransactionViewModel
{
    [Required]
    [Display(Name = "Thức ăn")]
    public int FeedItemId { get; set; }

    [Display(Name = "Đàn sử dụng")]
    public int? HerdId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Loại giao dịch")]
    public string TransactionType { get; set; } = "Xuất";

    [Range(0.01, 100000000)]
    [Display(Name = "Số lượng")]
    public decimal Quantity { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public List<SelectListItem> FeedItems { get; set; } = [];
    public List<SelectListItem> Herds { get; set; } = [];
}

public class ReportsViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Profit => TotalRevenue - TotalExpense;
    public int CurrentAnimals { get; set; }
    public int Mortality { get; set; }
    public decimal MortalityRate { get; set; }
    public List<GrowthRecord> Growth { get; set; } = [];
    public List<FeedItem> LowStockItems { get; set; } = [];
    public List<SaleRecord> RecentSales { get; set; } = [];
    public List<ExpenseRecord> RecentExpenses { get; set; } = [];
}

public class AiAnalysisResult
{
    public string Title { get; set; } = "Kết quả phân tích";
    public string Summary { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = [];
    public List<string> Reminders { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string Provider { get; set; } = "Phân tích nội bộ";
    public bool UsedExternalAi { get; set; }
}

public class AiPageViewModel
{
    [Required]
    [Display(Name = "Loại phân tích")]
    public string Task { get; set; } = "summary";

    [StringLength(1000)]
    [Display(Name = "Câu hỏi / yêu cầu bổ sung")]
    public string? Question { get; set; }

    public AiAnalysisResult? Result { get; set; }
    public List<AiLog> Logs { get; set; } = [];
    public bool ExternalAiConfigured { get; set; }
    public string PromptVersion { get; set; } = "v3";
}

public class UserCreateViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = RoleNames.Employee;
}

public class PromptLabViewModel
{
    [Required]
    [Display(Name = "Tác vụ dùng để so sánh")]
    public string Task { get; set; } = "summary";

    [StringLength(1000)]
    [Display(Name = "Câu hỏi kiểm thử dùng chung")]
    public string? Question { get; set; } = "Hãy tóm tắt tình trạng trang trại và nêu các việc cần ưu tiên trong 7 ngày tới.";

    public bool ExternalAiConfigured { get; set; }
    public List<PromptExperimentResult> Results { get; set; } = [];
}

public class PromptExperimentResult
{
    public string Version { get; set; } = string.Empty;
    public string PromptFile { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public AiAnalysisResult Result { get; set; } = new();
    public long ElapsedMilliseconds { get; set; }
}

public class AiApiRequest
{
    [StringLength(30)]
    public string Task { get; set; } = "summary";

    [StringLength(1000)]
    public string? Question { get; set; }
}
