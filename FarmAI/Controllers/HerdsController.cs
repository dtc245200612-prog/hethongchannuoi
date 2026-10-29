using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

public class HerdsController(FarmDbContext db,AccessScope scope,FarmService farms) : AppController
{
    public async Task<IActionResult> Index(string? q,Guid? barn,int page=1)
    {
        page=Math.Max(1,page);const int size=20;
        var query=scope.VisibleHerds(User);
        if(!string.IsNullOrWhiteSpace(q))query=query.Where(x=>x.Code.Contains(q)||x.Name.Contains(q));
        if(barn.HasValue)query=query.Where(x=>x.BarnId==barn);
        ViewBag.Herds=await query.OrderBy(x=>x.Code).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync();
        ViewBag.Page=page;ViewBag.Total=await query.CountAsync();ViewBag.Query=q;
        ViewBag.Barns=await scope.VisibleBarns(User).OrderBy(b=>b.Code).ToListAsync();
        return View();
    }
    public async Task<IActionResult> Details(Guid id)
    {
        var h=await scope.RequireHerd(User,id);
        ViewBag.Herd=h;ViewBag.Barns=await scope.VisibleBarns(User).ToListAsync();
        ViewBag.Movements=await db.HerdMovements.Where(m=>m.HerdId==id).OrderByDescending(x=>x.OccurredAt).Take(80).ToListAsync();
        ViewBag.Allocations=await db.Allocations.Where(x=>x.HerdId==id).OrderByDescending(x=>x.FromAt).ToListAsync();
        ViewBag.Records=await db.GrowthRecords.Where(x=>x.HerdId==id).OrderBy(x=>x.MeasuredAt).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(HerdForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Thông tin đàn chưa hợp lệ.");await farms.CreateHerd(User,form);},"/Herds");
    [HttpPost]public Task<IActionResult> Edit(Guid id,string name,string? breed,string? purpose)=>Save(()=>farms.UpdateHerd(User,id,name,breed,purpose),"/Herds/Details/"+id);
    [HttpPost]public Task<IActionResult> Archive(Guid id)=>Save(()=>farms.ArchiveHerd(User,id),"/Herds");
    [HttpPost]public Task<IActionResult> Transfer(TransferForm form)=>Save(()=>farms.Transfer(User,form),"/Herds/Details/"+form.HerdId);
    [HttpPost]public Task<IActionResult> Split(SplitForm form)=>Save(()=>farms.Split(User,form),"/Herds/Details/"+form.SourceHerdId);
    [HttpPost]public Task<IActionResult> Movement(MovementForm form)=>Save(()=>farms.RequestMovement(User,form),"/Herds/Details/"+form.HerdId);
    [HttpPost]public Task<IActionResult> ApproveMovement(Guid id,Guid herdId)=>Save(()=>farms.ApproveMovement(User,id),"/Herds/Details/"+herdId);
}
