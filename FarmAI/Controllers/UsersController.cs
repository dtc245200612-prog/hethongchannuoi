using FarmAI.Data;
using FarmAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

[Authorize(Roles = RoleNames.Manager)]
public class UsersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Users.AsNoTracking().OrderBy(x => x.Username).ToListAsync());

    public IActionResult Create() => View(new UserCreateViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!new[] { RoleNames.Manager, RoleNames.Technician, RoleNames.Employee }.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        if (await db.Users.AnyAsync(x => x.Username == model.Username))
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
        if (!ModelState.IsValid) return View(model);

        var user = new AppUser
        {
            Username = model.Username.Trim(),
            FullName = model.FullName.Trim(),
            Role = model.Role,
            IsActive = true
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, model.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Username == "admin" && user.IsActive)
        {
            TempData["Error"] = "Không thể khóa tài khoản admin mặc định.";
            return RedirectToAction(nameof(Index));
        }
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = user.IsActive ? "Đã kích hoạt tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
