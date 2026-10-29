using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
[Authorize(Roles=Roles.Manager)]
public class SalesController(FarmDbContext db,AccessScope scope,OperationsService ops) : AppController
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Herds=await scope.VisibleHerds(User).Where(x=>x.Status=="ACTIVE").ToListAsync();
        ViewBag.Sales=await db.Sales.Include(s=>s.Lines).Include(s=>s.Payments).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync();
        return View();
    }
    public async Task<IActionResult> Details(Guid id)
    {
        var s=await db.Sales.Include(x=>x.Lines).Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id);
        if(s==null)return NotFound();
        ViewBag.Sale=s;ViewBag.Herds=await scope.VisibleHerds(User).ToListAsync();return View();
    }
    [HttpPost]public Task<IActionResult> Create(SaleForm form)=>Save(async()=>{if(!ModelState.IsValid)throw new BusinessException("Thông tin phiếu bán chưa hợp lệ.");await ops.CreateSale(User,form);},"/Sales");
    [HttpPost]public Task<IActionResult> AddLine(LineForm form)=>Save(()=>ops.AddSaleLine(User,form),"/Sales/Details/"+form.SaleId);
    [HttpPost]public Task<IActionResult> Confirm(Guid id,Guid key)=>Save(()=>ops.ConfirmSale(User,id,key),"/Sales/Details/"+id);
    [HttpPost]public Task<IActionResult> Pay(PaymentForm form)=>Save(()=>ops.MakePayment(User,form),"/Sales/Details/"+form.SaleId);
}
