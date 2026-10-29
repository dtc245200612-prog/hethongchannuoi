using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;

public class CareController(FarmDbContext db,AccessScope scope,FarmService farms) : AppController
{
    public async Task<IActionResult> Index()
    {
        var ids=scope.VisibleHerds(User).Select(h=>h.Id);
        ViewBag.Herds=await scope.VisibleHerds(User).OrderBy(h=>h.Code).ToListAsync();
        ViewBag.Entries=await db.CareLogs.Where(l=>ids.Contains(l.HerdId)).OrderByDescending(x=>x.OccurredAt).Take(100).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(CareForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Biểu mẫu chăm sóc chưa hợp lệ.");await farms.CreateCare(User,form);},"/Care");
    [HttpPost]public Task<IActionResult> Approve(Guid id)=>Save(()=>farms.ApproveCare(User,id),"/Care");
}
