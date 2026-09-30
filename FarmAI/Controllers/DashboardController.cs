using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var model = new DashboardViewModel
        {
            HerdCount = await db.Herds.CountAsync(),
            AnimalCount = await db.Herds.SumAsync(x => x.Quantity),
            OverdueVaccineCount = await db.Vaccines.CountAsync(x => x.CompletedDate == null && x.ScheduledDate < today),
            Revenue = await db.Sales.SumAsync(x => x.Quantity * x.AverageWeight * x.UnitPrice),
            LowStockCount = await db.FeedItems.CountAsync(x => x.Quantity <= x.MinimumStock),
            HealthCheckCount = await db.CareLogs.CountAsync(x => x.RequiresHealthCheck && x.Date >= today.AddDays(-14)),
            Vaccines = await db.Vaccines.AsNoTracking().Include(x => x.Herd)
                .Where(x => x.CompletedDate == null)
                .OrderBy(x => x.ScheduledDate).Take(5).ToListAsync(),
            RecentCare = await db.CareLogs.AsNoTracking().Include(x => x.Herd)
                .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(5).ToListAsync(),
            Growth = await db.GrowthRecords.AsNoTracking().Include(x => x.Herd)
                .Where(x => x.Herd != null && x.Herd.Code == "DAN-BO-001")
                .OrderBy(x => x.RecordedDate).Take(12).ToListAsync()
        };

        return View(model);
    }


    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> ResetDemo()
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db);
        TempData["Success"] = "Đã khôi phục dữ liệu mẫu.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
