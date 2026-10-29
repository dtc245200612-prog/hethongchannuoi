using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
public class VaccinationsController(FarmDbContext db,AccessScope scope,OperationsService ops) : AppController
{
    public async Task<IActionResult> Index()
    {
        var ids=scope.VisibleHerds(User).Select(h=>h.Id);
        ViewBag.Herds=await scope.VisibleHerds(User).ToListAsync();
        ViewBag.Vaccines=await db.Vaccines.Where(v=>v.Active).ToListAsync();
        ViewBag.Users=await db.Users.Where(u=>u.Active).ToListAsync();
        var plans=await db.VaccinationPlans.Where(p=>ids.Contains(p.HerdId)).OrderBy(p=>p.PlannedDate).Take(100).ToListAsync();
        ViewBag.Plans=plans;
        ViewBag.Events=await db.VaccinationEvents.Where(e=>plans.Select(p=>p.Id).Contains(e.PlanId)).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Create(PlanForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Thông tin lịch tiêm không hợp lệ.");await ops.CreatePlan(User,form);},"/Vaccinations");
    [HttpPost]public Task<IActionResult> Submit(VaccinationEventForm form)=>Save(()=>ops.SubmitInjection(User,form),"/Vaccinations");
    [HttpPost]public Task<IActionResult> Approve(Guid id)=>Save(()=>ops.ApproveInjection(User,id),"/Vaccinations");
    [HttpPost]public Task<IActionResult> Adjust(Guid id,DateOnly? newDate,string reason,bool cancel=false)=>Save(()=>ops.AdjustPlan(User,id,newDate,reason,cancel),"/Vaccinations");
    [HttpPost]public Task<IActionResult> Refresh()=>Save(async()=>{await ops.RefreshAlerts();},"/Vaccinations");
}
