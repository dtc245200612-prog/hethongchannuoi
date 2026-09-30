using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class SalesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Sales.AsNoTracking().Include(x => x.Herd)
        .OrderByDescending(x => x.Date).ToListAsync());

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create() => View(await BuildForm(new SaleRecord { Date = DateTime.Today }));

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(GenericHerdFormViewModel<SaleRecord> model)
    {
        var herd = await db.Herds.FindAsync(model.Item.HerdId);
        if (herd is null) ModelState.AddModelError("Item.HerdId", "Đàn không hợp lệ.");
        else if (model.Item.Quantity > herd.Quantity) ModelState.AddModelError("Item.Quantity", "Số lượng xuất bán lớn hơn số lượng đàn hiện có.");

        if (!ModelState.IsValid)
        {
            model.Herds = await HerdOptions(model.Item.HerdId);
            return View(model);
        }

        herd!.Quantity -= model.Item.Quantity;
        db.Sales.Add(model.Item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi nhận xuất bán và cập nhật số lượng đàn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Sales.FindAsync(id);
        if (item is null) return NotFound();
        var herd = await db.Herds.FindAsync(item.HerdId);
        if (herd is not null) herd.Quantity += item.Quantity;
        db.Sales.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa giao dịch và hoàn lại số lượng đàn.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GenericHerdFormViewModel<SaleRecord>> BuildForm(SaleRecord item) => new()
    {
        Item = item,
        Herds = await HerdOptions(item.HerdId)
    };

    private async Task<List<SelectListItem>> HerdOptions(int selected = 0) =>
        await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name + $" · {x.Quantity} con", x.Id.ToString(), x.Id == selected))
            .ToListAsync();
}
