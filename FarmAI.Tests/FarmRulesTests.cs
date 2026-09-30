using Xunit;
using FarmAI.Services;

namespace FarmAI.Tests;

public class FarmRulesTests
{
    [Fact]
    public void Vaccine_Completed_IsCompleted()
    {
        var today = new DateTime(2026, 9, 22);
        var status = FarmRules.GetVaccineStatus(today.AddDays(-5), today.AddDays(-1), today);
        Assert.Equal("Đã tiêm", status);
    }

    [Fact]
    public void Vaccine_PastDue_IsOverdue()
    {
        var today = new DateTime(2026, 9, 22);
        var status = FarmRules.GetVaccineStatus(today.AddDays(-1), null, today);
        Assert.Equal("Quá hạn", status);
    }

    [Fact]
    public void Vaccine_WithinSevenDays_IsUpcoming()
    {
        var today = new DateTime(2026, 9, 22);
        var status = FarmRules.GetVaccineStatus(today.AddDays(7), null, today);
        Assert.Equal("Sắp đến hạn", status);
    }

    [Fact]
    public void Feed_Export_UpdatesStock()
    {
        var result = FarmRules.ApplyStockTransaction(100, "Xuất", 25);
        Assert.Equal(75, result);
    }

    [Fact]
    public void Feed_ExportOverStock_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FarmRules.ApplyStockTransaction(10, "Xuất", 11));
    }

    [Fact]
    public void MortalityRate_UsesCurrentPlusMortalityAsBase()
    {
        var rate = FarmRules.CalculateMortalityRate(94, 6);
        Assert.Equal(6m, rate);
    }

    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(99, 100, true)]
    [InlineData(101, 100, false)]
    public void LowStock_HandlesBoundary(int quantity, int minimum, bool expected)
    {
        Assert.Equal(expected, FarmRules.IsLowStock(quantity, minimum));
    }

    [Fact]
    public void CareSchedule_PastDue_IsOverdue()
    {
        var today = new DateTime(2026, 9, 22);
        Assert.Equal("Quá hạn", FarmRules.GetCareScheduleStatus(today.AddDays(-1), null, today));
    }

    [Fact]
    public void CareSchedule_Completed_IsCompleted()
    {
        var today = new DateTime(2026, 9, 22);
        Assert.Equal("Đã hoàn thành", FarmRules.GetCareScheduleStatus(today.AddDays(-2), today, today));
    }

    [Fact]
    public void SaleAmount_IsCalculatedFromQuantityWeightAndPrice()
    {
        Assert.Equal(4_500_000m, FarmRules.CalculateSaleAmount(10, 10m, 45_000m));
    }

    [Fact]
    public void GrowthChange_RequiresAtLeastTwoMeasurements()
    {
        Assert.Null(FarmRules.CalculateGrowthChange([212.4m]));
        Assert.Equal(14.9m, FarmRules.CalculateGrowthChange([212.4m, 219.8m, 227.3m]));
    }

    [Theory]
    [InlineData("Manager", true)]
    [InlineData("Technician", true)]
    [InlineData("Employee", false)]
    public void AiPermission_MatchesRoles(string role, bool expected)
    {
        Assert.Equal(expected, FarmRules.CanUseAi(role));
    }

}
