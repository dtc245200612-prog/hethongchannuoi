using System.Security.Claims;
using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

public class AccountController(FarmDbContext db) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Login()=>User.Identity?.IsAuthenticated==true?RedirectToAction("Index","Home"):View(new LoginForm());
    [AllowAnonymous,HttpPost,EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if(!ModelState.IsValid)return View(form);
        var u=await db.Users.SingleOrDefaultAsync(x=>x.Username==form.Username.Trim());
        var valid=u!=null && u.Active && new PasswordHasher<AppUser>().VerifyHashedPassword(u,u.PasswordHash,form.Password)!=PasswordVerificationResult.Failed;
        if(!valid){ModelState.AddModelError("","Sai tên đăng nhập hoặc mật khẩu.");return View(form);}
        var s=new LoginSession{UserId=u!.Id,ExpiresAt=DateTime.UtcNow.AddMinutes(15)};
        db.Sessions.Add(s);await db.SaveChangesAsync();
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),new Claim(ClaimTypes.Name,u.Username),new Claim(ClaimTypes.Role,u.Role),new Claim("session",s.Id.ToString())};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties{IsPersistent=false,ExpiresUtc=new DateTimeOffset(s.ExpiresAt,TimeSpan.Zero)});
        return RedirectToAction("Index","Home");
    }
    [Authorize,HttpPost]
    public async Task<IActionResult> Logout()
    {
        var sid=Guid.TryParse(User.FindFirstValue("session"),out var id)?id:Guid.Empty;
        var session=await db.Sessions.FindAsync(sid);if(session!=null){session.RevokedAt=DateTime.UtcNow;await db.SaveChangesAsync();}
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
    [AllowAnonymous] public IActionResult Denied()=>View();
}
