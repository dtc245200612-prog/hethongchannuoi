namespace FarmAI.Services;

public static class FarmRules
{
    public static string GetVaccineStatus(DateTime scheduledDate, DateTime? completedDate, DateTime today)
    {
        if (completedDate.HasValue) return "Đã tiêm";
        if (scheduledDate.Date < today.Date) return "Quá hạn";
        if (scheduledDate.Date <= today.Date.AddDays(7)) return "Sắp đến hạn";
        return "Chưa tiêm";
    }

    public static string GetCareScheduleStatus(DateTime scheduledDate, DateTime? completedDate, DateTime today)
    {
        if (completedDate.HasValue) return "Đã hoàn thành";
        if (scheduledDate.Date < today.Date) return "Quá hạn";
        if (scheduledDate.Date <= today.Date.AddDays(7)) return "Sắp đến hạn";
        return "Đã lên lịch";
    }

    public static decimal ApplyStockTransaction(decimal currentQuantity, string transactionType, decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (transactionType == "Nhập") return currentQuantity + quantity;
        if (transactionType == "Xuất")
        {
            if (quantity > currentQuantity) throw new InvalidOperationException("Số lượng xuất lớn hơn tồn kho.");
            return currentQuantity - quantity;
        }
        throw new ArgumentException("Loại giao dịch không hợp lệ.", nameof(transactionType));
    }

    public static decimal CalculateMortalityRate(int currentAnimals, int mortality)
    {
        if (currentAnimals < 0 || mortality < 0) throw new ArgumentOutOfRangeException();
        var totalBase = currentAnimals + mortality;
        return totalBase == 0 ? 0 : Math.Round((decimal)mortality / totalBase * 100, 2);
    }

    public static bool IsLowStock(decimal quantity, decimal minimumStock) => quantity <= minimumStock;

    public static decimal CalculateSaleAmount(int quantity, decimal averageWeight, decimal unitPrice)
    {
        if (quantity < 0 || averageWeight < 0 || unitPrice < 0) throw new ArgumentOutOfRangeException();
        return quantity * averageWeight * unitPrice;
    }

    public static decimal? CalculateGrowthChange(IEnumerable<decimal> weights)
    {
        var values = weights.ToList();
        if (values.Count < 2) return null;
        return values[^1] - values[0];
    }

    public static bool CanUseAi(string role) => role is FarmAI.Models.RoleNames.Manager or FarmAI.Models.RoleNames.Technician;
}
