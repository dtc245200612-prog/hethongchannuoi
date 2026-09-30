using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class VaccinesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Vaccines.AsNoTracking().Include(x => x.Herd)
        .OrderBy(x => x.CompletedDate != null).ThenBy(x => x.ScheduledDate).ToListAsync());

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create() => View(await BuildForm(new VaccineRecord { ScheduledDate = DateTime.Today.AddDays(7) }));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(GenericHerdFormViewModel<VaccineRecord> model)
    {
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        NormalizeStatus(model.Item);
        db.Vaccines.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm lịch tiêm phòng.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Vaccines.FindAsync(id);
        return item is null ? NotFound() : View(await BuildForm(item));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, GenericHerdFormViewModel<VaccineRecord> model)
    {
        if (id != model.Item.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        NormalizeStatus(model.Item);
        db.Vaccines.Update(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật lịch tiêm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Complete(int id)
    {
        var item = await db.Vaccines.FindAsync(id);
        if (item is null) return NotFound();
        item.CompletedDate = DateTime.Today;
        item.Status = "Đã tiêm";
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã đánh dấu lịch tiêm hoàn thành.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Vaccines.FindAsync(id);
        if (item is null) return NotFound();
        db.Vaccines.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa lịch tiêm.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeStatus(VaccineRecord item)
    {
        item.Status = FarmRules.GetVaccineStatus(item.ScheduledDate, item.CompletedDate, DateTime.Today);
    }

    private async Task<GenericHerdFormViewModel<VaccineRecord>> BuildForm(VaccineRecord item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<List<SelectListItem>> HerdOptions(int selected = 0) =>
        await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
