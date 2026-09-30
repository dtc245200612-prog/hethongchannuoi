using System.Text.Json;
using FarmAI.Data;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Services;

public sealed class FarmDataService(AppDbContext db)
{
    public async Task<string> BuildAiContextAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.Today.AddDays(-14);
        var until = DateTime.Today.AddDays(7);

        var data = new
        {
            generatedAt = DateTime.Now,
            scope = "Dữ liệu quản lý trang trại; nhật ký gần 14 ngày; lịch vaccine đến 7 ngày tới và các lịch quá hạn chưa hoàn thành.",
            herds = await db.Herds.AsNoTracking()
                .Include(x => x.Barn)
                .Select(x => new
                {
                    x.Code,
                    x.Name,
                    x.Species,
                    x.Breed,
                    barn = x.Barn != null ? x.Barn.Name : "",
                    x.Quantity,
                    x.Status,
                    x.MortalityCount
                }).ToListAsync(cancellationToken),
            barns = await db.Barns.AsNoTracking()
                .Select(x => new { x.Code, x.Name, x.Capacity, x.Temperature, x.Humidity, x.Status })
                .ToListAsync(cancellationToken),
            careLogs = await db.CareLogs.AsNoTracking()
                .Where(x => x.Date >= since)
                .OrderByDescending(x => x.Date)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.Date,
                    x.Type,
                    x.Description,
                    x.RequiresHealthCheck
                }).ToListAsync(cancellationToken),
            careSchedules = await db.CareSchedules.AsNoTracking()
                .Where(x => (x.CompletedDate == null && x.ScheduledDate <= until) || x.CompletedDate >= since)
                .OrderBy(x => x.ScheduledDate)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.Title,
                    x.ScheduledDate,
                    x.CompletedDate,
                    x.Status,
                    x.Assignee
                }).ToListAsync(cancellationToken),
            vaccines = await db.Vaccines.AsNoTracking()
                .Where(x => (x.CompletedDate == null && x.ScheduledDate <= until) || x.CompletedDate >= since)
                .OrderBy(x => x.ScheduledDate)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.VaccineName,
                    x.ScheduledDate,
                    x.CompletedDate,
                    x.Status
                }).ToListAsync(cancellationToken),
            growth = await db.GrowthRecords.AsNoTracking()
                .Where(x => x.RecordedDate >= since)
                .OrderBy(x => x.RecordedDate)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.RecordedDate,
                    x.AverageWeight,
                    x.SampleSize
                }).ToListAsync(cancellationToken),
            feed = await db.FeedItems.AsNoTracking()
                .Select(x => new { x.Name, x.Unit, x.Quantity, x.MinimumStock })
                .ToListAsync(cancellationToken),
            medicines = await db.MedicineItems.AsNoTracking()
                .Select(x => new { x.Name, x.Unit, x.Quantity, x.MinimumStock, x.ExpiryDate })
                .ToListAsync(cancellationToken),
            reproduction = await db.ReproductionRecords.AsNoTracking()
                .Where(x => x.Date >= since || x.NextExpectedDate <= until)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.EventType,
                    x.Date,
                    x.Result,
                    x.NextExpectedDate
                }).ToListAsync(cancellationToken),
            sales = await db.Sales.AsNoTracking()
                .Where(x => x.Date >= since)
                .Include(x => x.Herd)
                .Select(x => new
                {
                    herd = x.Herd != null ? x.Herd.Code : "",
                    x.Date,
                    x.Quantity,
                    x.AverageWeight,
                    x.UnitPrice
                }).ToListAsync(cancellationToken),
            expenses = await db.Expenses.AsNoTracking()
                .Where(x => x.Date >= since)
                .Select(x => new { x.Date, x.Category, x.Description, x.Amount })
                .ToListAsync(cancellationToken)
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
