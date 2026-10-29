using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
public class ReproductionController(FarmDbContext db,AccessScope scope,FarmService farms) : AppController
{
    public async Task<IActionResult> Index()
    {
        var ids=scope.VisibleHerds(User).Select(x=>x.Id);
        ViewBag.Herds=await scope.VisibleHerds(User).ToListAsync();
        ViewBag.Animals=await db.Animals.Where(x=>ids.Contains(x.HerdId)).ToListAsync();
        ViewBag.Events=await db.ReproductionEvents.Where(x=>ids.Contains(x.TargetHerdId)).OrderByDescending(x=>x.EventDate).Take(50).ToListAsync();
        return View();
    }
    [HttpPost]public Task<IActionResult> Animal(AnimalForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Cá thể chưa hợp lệ.");await farms.AddAnimal(User,form);},"/Reproduction");
    [HttpPost]public Task<IActionResult> Birth(ReproductionForm form)=>Save(()=>farms.RegisterBirth(User,form),"/Reproduction");
}
