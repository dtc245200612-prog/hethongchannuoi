using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize(Roles = RoleNames.Manager)]
public class ExpensesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Expenses.AsNoTracking().OrderByDescending(x => x.Date).ToListAsync());

    public IActionResult Create() => View(new ExpenseRecord { Date = DateTime.Today });

    [HttpPost]
    public async Task<IActionResult> Create(ExpenseRecord item)
    {
        if (!ModelState.IsValid) return View(item);
        db.Expenses.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi nhận chi phí.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Expenses.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ExpenseRecord item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid) return View(item);
        db.Expenses.Update(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật chi phí.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Expenses.FindAsync(id);
        if (item is null) return NotFound();
        db.Expenses.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa chi phí.";
        return RedirectToAction(nameof(Index));
    }
}
