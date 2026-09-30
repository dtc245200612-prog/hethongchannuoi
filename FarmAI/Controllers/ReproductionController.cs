using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class ReproductionController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.ReproductionRecords.AsNoTracking().Include(x => x.Herd)
        .OrderByDescending(x => x.Date).ToListAsync());

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create() => View(await BuildForm(new ReproductionRecord { Date = DateTime.Today }));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(GenericHerdFormViewModel<ReproductionRecord> model)
    {
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        db.ReproductionRecords.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi nhận sinh sản.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ReproductionRecords.FindAsync(id);
        return item is null ? NotFound() : View(await BuildForm(item));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, GenericHerdFormViewModel<ReproductionRecord> model)
    {
        if (id != model.Item.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }
        db.ReproductionRecords.Update(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật bản ghi sinh sản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ReproductionRecords.FindAsync(id);
        if (item is null) return NotFound();
        db.ReproductionRecords.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa bản ghi sinh sản.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GenericHerdFormViewModel<ReproductionRecord>> BuildForm(ReproductionRecord item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<List<SelectListItem>> HerdOptions(int selected = 0) =>
        await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
