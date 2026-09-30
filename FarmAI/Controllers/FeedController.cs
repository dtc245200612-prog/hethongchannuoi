using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize]
public class FeedController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new FeedIndexViewModel
        {
            Items = await db.FeedItems.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Transactions = await db.FeedTransactions.AsNoTracking().Include(x => x.FeedItem).Include(x => x.Herd)
                .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(50).ToListAsync(),
            Medicines = await db.MedicineItems.AsNoTracking().OrderBy(x => x.Name).ToListAsync()
        };
        return View(model);
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public IActionResult CreateItem() => View(new FeedItem());

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> CreateItem(FeedItem item)
    {
        if (!ModelState.IsValid) return View(item);
        db.FeedItems.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm thức ăn vào kho.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> EditItem(int id)
    {
        var item = await db.FeedItems.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> EditItem(int id, FeedItem item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid) return View(item);
        db.FeedItems.Update(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật tồn kho thức ăn.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Transaction() => View(await BuildTransactionForm(new FeedTransactionViewModel()));

    [HttpPost]
    public async Task<IActionResult> Transaction(FeedTransactionViewModel model)
    {
        if (model.TransactionType is not ("Nhập" or "Xuất"))
            ModelState.AddModelError(nameof(model.TransactionType), "Loại giao dịch không hợp lệ.");

        var item = await db.FeedItems.FindAsync(model.FeedItemId);
        if (item is null)
            ModelState.AddModelError(nameof(model.FeedItemId), "Thức ăn không tồn tại.");
        else if (model.TransactionType == "Xuất" && model.Quantity > item.Quantity)
            ModelState.AddModelError(nameof(model.Quantity), "Số lượng xuất lớn hơn tồn kho hiện tại.");

        if (!ModelState.IsValid)
            return View(await BuildTransactionForm(model));

        item!.Quantity = FarmRules.ApplyStockTransaction(item.Quantity, model.TransactionType, model.Quantity);

        db.FeedTransactions.Add(new FeedTransaction
        {
            FeedItemId = model.FeedItemId,
            HerdId = model.HerdId,
            Date = model.Date,
            TransactionType = model.TransactionType,
            Quantity = model.Quantity,
            Notes = model.Notes
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi giao dịch kho thức ăn.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public IActionResult CreateMedicine() => View(new MedicineItem());

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> CreateMedicine(MedicineItem item)
    {
        if (!ModelState.IsValid) return View(item);
        db.MedicineItems.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm thuốc/vật tư.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> EditMedicine(int id)
    {
        var item = await db.MedicineItems.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager + "," + RoleNames.Technician)]
    public async Task<IActionResult> EditMedicine(int id, MedicineItem item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid) return View(item);
        db.MedicineItems.Update(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thuốc/vật tư.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await db.FeedItems.FindAsync(id);
        if (item is null) return NotFound();
        db.FeedItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa mặt hàng thức ăn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RoleNames.Manager)]
    public async Task<IActionResult> DeleteMedicine(int id)
    {
        var item = await db.MedicineItems.FindAsync(id);
        if (item is null) return NotFound();
        db.MedicineItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa thuốc/vật tư.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<FeedTransactionViewModel> BuildTransactionForm(FeedTransactionViewModel model)
    {
        model.FeedItems = await db.FeedItems.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name + $" · tồn {x.Quantity} {x.Unit}", x.Id.ToString(), x.Id == model.FeedItemId))
            .ToListAsync();
        model.Herds = await db.Herds.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString(), x.Id == model.HerdId))
            .ToListAsync();
        return model;
    }
}
