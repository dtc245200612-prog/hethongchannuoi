using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
[Authorize(Roles=Roles.Manager)]
public class AdminController(FarmDbContext db) : AppController
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Users=await db.Users.OrderBy(u=>u.Username).ToListAsync();
        ViewBag.Barns=await db.Barns.OrderBy(x=>x.Code).ToListAsync();
        ViewBag.Assignments=await db.Assignments.Where(x=>x.ValidTo==null).ToListAsync();
        ViewBag.Audits=await db.AuditLogs.OrderByDescending(x=>x.CreatedAt).Take(80).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(UserForm f)=>Save(async()=>{
        if(!ModelState.IsValid||f.Role is not (Roles.Manager or Roles.Technician or Roles.Employee))throw new BusinessException("Tài khoản chưa hợp lệ.");
        if(await db.Users.AnyAsync(x=>x.Username==f.Username))throw new BusinessException("Tên đăng nhập đã tồn tại.");
        var u=new AppUser{Username=f.Username,Role=f.Role};u.PasswordHash=new PasswordHasher<AppUser>().HashPassword(u,f.Password);
        db.Users.Add(u);db.AuditLogs.Add(new AuditLog{ActorId=User.UserId(),Action="USER_CREATED",EntityType="AppUser",EntityId=u.Id});await db.SaveChangesAsync();
    },"/Admin");
    [HttpPost]public Task<IActionResult> ChangeUser(Guid id,string role,bool active)=>Save(async()=>{
        if(role is not (Roles.Manager or Roles.Technician or Roles.Employee))throw new BusinessException("Vai trò không hợp lệ.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var u=await db.Users.FindAsync(id)??throw new BusinessException("Tài khoản không tồn tại.");
        if(u.Role==Roles.Manager && (!active||role!=Roles.Manager) && await db.Users.CountAsync(x=>x.Role==Roles.Manager&&x.Active)<=1)
            throw new BusinessException("Không được vô hiệu hóa quản lý cuối cùng.");
        u.Role=role;u.Active=active;
        var sessions=await db.Sessions.Where(x=>x.UserId==id&&x.RevokedAt==null).ToListAsync();
        foreach(var s in sessions)s.RevokedAt=DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog{ActorId=User.UserId(),Action="USER_ROLE_CHANGED",EntityType="AppUser",EntityId=id,ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{role,active})});
        await db.SaveChangesAsync();await tx.CommitAsync();
    },"/Admin");
    [HttpPost]public Task<IActionResult> Assign(Guid userId,Guid barnId,bool remove=false)=>Save(async()=>{
        if(!await db.Users.AnyAsync(x=>x.Id==userId)||!await db.Barns.AnyAsync(x=>x.Id==barnId))throw new BusinessException("Người dùng hoặc chuồng không tồn tại.");
        var a=await db.Assignments.FindAsync(userId,barnId);
        if(remove){if(a!=null)a.ValidTo=DateTime.UtcNow;}
        else if(a==null)db.Assignments.Add(new Assignment{UserId=userId,BarnId=barnId});else{a.ValidFrom=DateTime.UtcNow;a.ValidTo=null;}
        db.AuditLogs.Add(new AuditLog{ActorId=User.UserId(),Action=remove?"UNASSIGN_BARN":"ASSIGN_BARN",EntityType="Assignment",EntityId=barnId,ChangesJson=System.Text.Json.JsonSerializer.Serialize(new{userId})});
        await db.SaveChangesAsync();
    },"/Admin");
}
