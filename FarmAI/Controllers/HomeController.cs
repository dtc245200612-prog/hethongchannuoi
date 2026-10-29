using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FarmAI.Controllers;

public class HomeController(FarmDbContext db,AccessScope scope,ReportService reports) : AppController
{
    public async Task<IActionResult> Index()
    {
        var end=DateOnly.FromDateTime(DateTime.Today);var start=end.AddDays(-30);
        ViewBag.Summary=JsonSerializer.Serialize(await reports.Dashboard(User,start,end));
        ViewBag.Herds=await scope.VisibleHerds(User).OrderBy(x=>x.Code).Take(10).ToListAsync();
        var ids=scope.VisibleHerds(User).Select(x=>x.Id);
        var plans=await db.VaccinationPlans.Where(p=>ids.Contains(p.HerdId) && (p.Status=="PLANNED"||p.Status=="PARTIAL")&&p.PlannedDate<=end.AddDays(3))
            .OrderBy(p=>p.PlannedDate).Take(20).ToListAsync();
        ViewBag.Plans=plans;
        var manager=User.IsInRole(Roles.Manager);var userId=User.UserId();
        ViewBag.Notices=await db.Notifications.Where(n=>manager||n.RecipientId==userId)
            .OrderByDescending(n=>n.RemindDate).Take(8).ToListAsync();
        return View();
    }
    [AllowAnonymous]public IActionResult Error()=>View();
}
