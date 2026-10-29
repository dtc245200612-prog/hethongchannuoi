namespace FarmAI.Models;

// Pure calculations used by services and independently tested.
public static class FarmRules
{
    public static int Remaining(int current, int delta)
    {
        if ((long)current + delta < 0 || (long)current + delta > int.MaxValue)
            throw new ArgumentException("Số lượng đàn không hợp lệ.");
        return checked(current + delta);
    }
    public static decimal LineTotal(string basis, int count, decimal? weightKg, decimal unitPrice)
    {
        if (count <= 0 || unitPrice <= 0) throw new ArgumentException("Số lượng và giá phải dương.");
        var units = basis == "HEAD" ? count : basis == "KG" && weightKg > 0
            ? weightKg.Value : throw new ArgumentException("Cơ sở giá hoặc khối lượng không hợp lệ.");
        return Math.Round(units * unitPrice, 0, MidpointRounding.AwayFromZero);
    }
    public static decimal? Adg(GrowthRecord? previous, GrowthRecord? current)
    {
        if (previous == null || current == null || previous.SampleSize <= 0 || current.SampleSize <= 0 ||
            previous.Method != current.Method || current.MeasuredAt <= previous.MeasuredAt) return null;
        var days = (decimal)(current.MeasuredAt - previous.MeasuredAt).TotalDays;
        return Math.Round((current.AverageKg - previous.AverageKg) / days, 3);
    }
    public static decimal? MortalityPercent(int deaths, int opening, int additions)
    {
        int baseCount = opening + additions;
        return baseCount > 0 ? Math.Round(deaths * 100m / baseCount, 2) : null;
    }
}
