using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

public class BarnsController(FarmDbContext db,AccessScope scope,FarmService farms) : AppController
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Barns=await scope.VisibleBarns(User).OrderBy(x=>x.Code).ToListAsync();
        ViewBag.Counts=await db.Herds.GroupBy(h=>h.BarnId).Select(g=>new{BarnId=g.Key,Count=g.Sum(h=>h.Quantity)}).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(BarnForm form)=>Save(async()=>{
        if(!ModelState.IsValid)throw new BusinessException("Vui lòng nhập đúng thông tin chuồng.");
        await farms.CreateBarn(User,form);
    },"/Barns");
    [HttpPost]public Task<IActionResult> Update(Guid id,int capacity,string status)=>Save(()=>farms.UpdateBarn(User,id,capacity,status),"/Barns");
}
