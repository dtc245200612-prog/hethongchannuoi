using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class BarnsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Barns.AsNoTracking().Include(x => x.Herds).OrderBy(x => x.Code).ToListAsync());

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public IActionResult Create() => View(new Barn());

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(Barn item)
    {
        if (await db.Barns.AnyAsync(x => x.Code == item.Code))
            ModelState.AddModelError(nameof(item.Code), "Mã chuồng đã tồn tại.");
        if (!ModelState.IsValid) return View(item);
        db.Barns.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm chuồng trại.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Barns.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id, Barn item)
    {
        if (id != item.Id) return BadRequest();
        if (await db.Barns.AnyAsync(x => x.Code == item.Code && x.Id != id))
            ModelState.AddModelError(nameof(item.Code), "Mã chuồng đã tồn tại.");
        if (!ModelState.IsValid) return View(item);
        db.Barns.Update(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật chuồng trại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await db.Herds.AnyAsync(x => x.BarnId == id))
        {
            TempData["Error"] = "Không thể xóa chuồng đang có đàn vật nuôi.";
            return RedirectToAction(nameof(Index));
        }
        var item = await db.Barns.FindAsync(id);
        if (item is null) return NotFound();
        db.Barns.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa chuồng.";
        return RedirectToAction(nameof(Index));
    }
}
