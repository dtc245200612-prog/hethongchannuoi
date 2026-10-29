using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
public class GrowthController(FarmDbContext db,AccessScope scope,FarmService farms) : AppController
{
    public async Task<IActionResult> Index(Guid? herdId)
    {
        var herds=await scope.VisibleHerds(User).OrderBy(h=>h.Code).ToListAsync();
        ViewBag.Herds=herds;
        var id=herdId??herds.FirstOrDefault()?.Id;
        ViewBag.Selected=id;
        var records=id.HasValue?await db.GrowthRecords.Where(x=>x.HerdId==id.Value).OrderBy(x=>x.MeasuredAt).ToListAsync():new List<GrowthRecord>();
        ViewBag.Records=records;
        ViewBag.ADG=records.Count>=2?FarmRules.Adg(records[^2],records[^1]):null;
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(GrowthForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Mẫu cân không hợp lệ.");await farms.AddGrowth(User,form);},"/Growth?herdId="+form.HerdId);
}
