using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
public class ReportsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var current = await db.Herds.SumAsync(x => x.Quantity);
        var mortality = await db.Herds.SumAsync(x => x.MortalityCount);
        var model = new ReportsViewModel
        {
            TotalRevenue = await db.Sales.SumAsync(x => x.Quantity * x.AverageWeight * x.UnitPrice),
            TotalExpense = await db.Expenses.SumAsync(x => x.Amount),
            CurrentAnimals = current,
            Mortality = mortality,
            MortalityRate = FarmRules.CalculateMortalityRate(current, mortality),
            Growth = await db.GrowthRecords.AsNoTracking().Include(x => x.Herd)
                .OrderBy(x => x.RecordedDate).ToListAsync(),
            LowStockItems = await db.FeedItems.AsNoTracking().Where(x => x.Quantity <= x.MinimumStock)
                .OrderBy(x => x.Quantity).ToListAsync(),
            RecentSales = await db.Sales.AsNoTracking().Include(x => x.Herd)
                .OrderByDescending(x => x.Date).Take(10).ToListAsync(),
            RecentExpenses = await db.Expenses.AsNoTracking().OrderByDescending(x => x.Date).Take(10).ToListAsync()
        };
        return View(model);
    }
}
