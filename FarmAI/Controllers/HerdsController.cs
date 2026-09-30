using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class HerdsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q = null)
    {
        var query = db.Herds.AsNoTracking().Include(x => x.Barn).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Code.Contains(q) || x.Name.Contains(q) || x.Species.Contains(q));
        ViewBag.Query = q;
        return View(await query.OrderBy(x => x.Code).ToListAsync());
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create() => View(await BuildForm(new Herd()));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(HerdFormViewModel model)
    {
        if (await db.Herds.AnyAsync(x => x.Code == model.Herd.Code))
            ModelState.AddModelError("Herd.Code", "Mã đàn đã tồn tại.");
        if (!await db.Barns.AnyAsync(x => x.Id == model.Herd.BarnId))
            ModelState.AddModelError("Herd.BarnId", "Chuồng không hợp lệ.");

        if (!ModelState.IsValid)
        {
            model.Barns = await BarnOptions(model.Herd.BarnId);
            return View(model);
        }

        db.Herds.Add(model.Herd);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm đàn vật nuôi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Herds.FindAsync(id);
        return item is null ? NotFound() : View(await BuildForm(item));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, HerdFormViewModel model)
    {
        if (id != model.Herd.Id) return BadRequest();
        if (await db.Herds.AnyAsync(x => x.Code == model.Herd.Code && x.Id != id))
            ModelState.AddModelError("Herd.Code", "Mã đàn đã tồn tại.");

        if (!ModelState.IsValid)
        {
            model.Barns = await BarnOptions(model.Herd.BarnId);
            return View(model);
        }

        db.Herds.Update(model.Herd);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật đàn vật nuôi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Herds.FindAsync(id);
        if (item is null) return NotFound();
        db.Herds.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa đàn và các bản ghi liên quan.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<HerdFormViewModel> BuildForm(Herd herd) => new()
    {
        Herd = herd,
        Barns = await BarnOptions(herd.BarnId)
    };

    private async Task<List<SelectListItem>> BarnOptions(int selected = 0) =>
        await db.Barns.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
