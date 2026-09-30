using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class CareLogsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new CareIndexViewModel
        {
            Logs = await db.CareLogs.AsNoTracking().Include(x => x.Herd)
                .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToListAsync(),
            Schedules = await db.CareSchedules.AsNoTracking().Include(x => x.Herd)
                .OrderBy(x => x.CompletedDate != null).ThenBy(x => x.ScheduledDate).ToListAsync()
        };
        return View(model);
    }

    public async Task<IActionResult> Create() => View(await BuildLogForm(new CareLog
    {
        Date = DateTime.Today,
        StaffName = User.Identity?.Name ?? string.Empty
    }));

    [HttpPost]
    public async Task<IActionResult> Create(GenericHerdFormViewModel<CareLog> model)
    {
        if (!await db.Herds.AnyAsync(x => x.Id == model.Item.HerdId))
            ModelState.AddModelError("Item.HerdId", "Đàn không hợp lệ.");
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Item.StaffName))
            model.Item.StaffName = User.Identity?.Name ?? "Nhân viên";
        db.CareLogs.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi nhật ký chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CareLogs.FindAsync(id);
        return item is null ? NotFound() : View(await BuildLogForm(item));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, GenericHerdFormViewModel<CareLog> model)
    {
        if (id != model.Item.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        db.CareLogs.Update(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật nhật ký.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CareLogs.FindAsync(id);
        if (item is null) return NotFound();
        db.CareLogs.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa nhật ký.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> CreateSchedule() => View(await BuildScheduleForm(new CareSchedule
    {
        ScheduledDate = DateTime.Today.AddDays(1),
        Assignee = User.Identity?.Name
    }));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> CreateSchedule(GenericHerdFormViewModel<CareSchedule> model)
    {
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        model.Item.Status = FarmRules.GetCareScheduleStatus(model.Item.ScheduledDate, model.Item.CompletedDate, DateTime.Today);
        db.CareSchedules.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm lịch chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> CompleteSchedule(int id)
    {
        var item = await db.CareSchedules.FindAsync(id);
        if (item is null) return NotFound();
        item.CompletedDate = DateTime.Today;
        item.Status = FarmRules.GetCareScheduleStatus(item.ScheduledDate, item.CompletedDate, DateTime.Today);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã hoàn thành lịch chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var item = await db.CareSchedules.FindAsync(id);
        if (item is null) return NotFound();
        db.CareSchedules.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa lịch chăm sóc.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GenericHerdFormViewModel<CareLog>> BuildLogForm(CareLog item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<GenericHerdFormViewModel<CareSchedule>> BuildScheduleForm(CareSchedule item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<List<SelectListItem>> HerdOptions(int selected = 0) =>
        await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
