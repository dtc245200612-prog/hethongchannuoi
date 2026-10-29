using FarmAI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Data;

public static class DemoSeed
{
    public static async Task InitializeAsync(IServiceProvider services,IConfiguration config,IHostEnvironment env)
    {
        using var scope=services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<FarmDbContext>();
        // Demo/bootstrap only. Use EF migrations and controlled credentials before real deployment.
        // EnsureCreated is intentionally demo-only; run EF migrations for production.
        if(!env.IsDevelopment())throw new InvalidOperationException("This package is a development demo; deploy with reviewed EF migrations before production.");
        await db.Database.EnsureCreatedAsync();
        if(await db.Users.AnyAsync())return;
        var pwd=config["FarmAI:SeedPassword"];
        if(string.IsNullOrWhiteSpace(pwd))
        {
            if(!env.IsDevelopment())throw new InvalidOperationException("FARMAI_SEED_PASSWORD is required for bootstrap.");
            pwd="FarmAI@2026";
        }
        var hasher=new PasswordHasher<AppUser>();
        AppUser User(string name,string role)
        {
            var u=new AppUser{Username=name,Role=role};u.PasswordHash=hasher.HashPassword(u,pwd);return u;
        }
        var manager=User("quanly",Roles.Manager);var tech=User("kythuat",Roles.Technician);var staff=User("nhanvien",Roles.Employee);
        var b1=new Barn{Code="C01",Name="Chuồng nuôi số 1",Species="Heo",Capacity=250};
        var b2=new Barn{Code="C02",Name="Chuồng nuôi số 2",Species="Heo",Capacity=180};
        var h1=new Herd{Code="D01",Name="Đàn heo thương phẩm",Species="Heo",Breed="Landrace",BarnId=b1.Id,Quantity=100,ReceivedDate=DateOnly.FromDateTime(DateTime.Today.AddDays(-60))};
        var h2=new Herd{Code="D02",Name="Đàn heo hậu bị",Species="Heo",Breed="Yorkshire",BarnId=b2.Id,Quantity=40,ReceivedDate=DateOnly.FromDateTime(DateTime.Today.AddDays(-45))};
        db.Users.AddRange(manager,tech,staff);db.Barns.AddRange(b1,b2);db.Herds.AddRange(h1,h2);
        db.Assignments.AddRange(new Assignment{UserId=tech.Id,BarnId=b1.Id},new Assignment{UserId=tech.Id,BarnId=b2.Id},new Assignment{UserId=staff.Id,BarnId=b1.Id});
        foreach(var h in new[]{h1,h2})
        {
            db.Allocations.Add(new Allocation{HerdId=h.Id,BarnId=h.BarnId});
            db.HerdMovements.Add(new HerdMovement{HerdId=h.Id,Kind="RECEIPT",Delta=h.Quantity,SourceId=h.Id,Reason="Dữ liệu demo",CreatedBy=manager.Id,ApprovedBy=manager.Id,Approved=true});
        }
        db.Vaccines.AddRange(new Vaccine{Name="Vaccine mẫu A"},new Vaccine{Name="Vaccine mẫu B"});
        db.GrowthRecords.AddRange(
            new GrowthRecord{HerdId=h1.Id,SampleSize=10,WeightKg=200,MeasuredAt=DateTime.UtcNow.AddDays(-15),CreatedBy=tech.Id},
            new GrowthRecord{HerdId=h1.Id,SampleSize=10,WeightKg=270,MeasuredAt=DateTime.UtcNow,CreatedBy=tech.Id});
        db.Animals.Add(new Animal{HerdId=h2.Id,Tag="ME-001",Sex="FEMALE"});
        await db.SaveChangesAsync();
    }
}
