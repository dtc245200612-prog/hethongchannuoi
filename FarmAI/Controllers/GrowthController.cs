using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class GrowthController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? herdId = null)
    {
        var query = db.GrowthRecords.AsNoTracking().Include(x => x.Herd).AsQueryable();
        if (herdId.HasValue) query = query.Where(x => x.HerdId == herdId.Value);
        ViewBag.HerdId = herdId;
        ViewBag.Herds = await HerdOptions(herdId ?? 0);
        return View(await query.OrderByDescending(x => x.RecordedDate).ThenByDescending(x => x.Id).ToListAsync());
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create() => View(await BuildForm(new GrowthRecord { RecordedDate = DateTime.Today, SampleSize = 1 }));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(GenericHerdFormViewModel<GrowthRecord> model)
    {
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        db.GrowthRecords.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi nhận tăng trưởng.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.GrowthRecords.FindAsync(id);
        return item is null ? NotFound() : View(await BuildForm(item));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, GenericHerdFormViewModel<GrowthRecord> model)
    {
        if (id != model.Item.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        db.GrowthRecords.Update(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật bản ghi tăng trưởng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.GrowthRecords.FindAsync(id);
        if (item is null) return NotFound();
        db.GrowthRecords.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa bản ghi tăng trưởng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GenericHerdFormViewModel<GrowthRecord>> BuildForm(GrowthRecord item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<List<SelectListItem>> HerdOptions(int selected = 0) =>
        await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
