using System.Text.Json;
using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Controllers;
public class ReportsController(FarmDbContext db,ReportService reports,AccessScope scope,OpenAiReportClient openAi) : AppController
{
    public async Task<IActionResult> Index(DateOnly? start,DateOnly? end)
    {
        var to=end??DateOnly.FromDateTime(DateTime.Today);var from=start??to.AddDays(-30);
        try{ViewBag.Dashboard=JsonSerializer.Serialize(await reports.Dashboard(User,from,to));}
        catch(BusinessException ex){ViewBag.Dashboard=ex.Message;}
        ViewBag.AiReportMode=openAi.Enabled
            ? "OpenAI đã được cấu hình: "+openAi.Model+" (kiểm tra kết nối khi tạo báo cáo)."
            : "Chưa có OpenAI API key. Hệ thống sẽ tạo báo cáo theo quy tắc RULE_BASED nếu chưa cấu hình Ollama.";
        ViewBag.Start=from;ViewBag.End=to;
        ViewBag.Herds=await scope.VisibleHerds(User).ToListAsync();
        var ids=scope.VisibleHerds(User).Select(x=>x.Id);
        ViewBag.Reports=await db.AiReports.Join(db.Snapshots,r=>r.SnapshotId,s=>s.Id,(r,s)=>new{r,s})
            .Where(x=>ids.Contains(x.s.HerdId)).OrderByDescending(x=>x.r.CreatedAt).Take(30).Select(x=>x.r).ToListAsync();
        return View();
    }
    [HttpPost,EnableRateLimiting("ai")]
    public Task<IActionResult> Generate(Guid herdId,DateOnly start,DateOnly end,string task)=>Save(async()=>{await reports.Generate(User,herdId,start,end,task);},"/Reports");
    public async Task<IActionResult> Details(Guid id)
    {
        var r=await db.AiReports.FindAsync(id);if(r==null)return NotFound();
        var snap=await db.Snapshots.FindAsync(r.SnapshotId);if(snap==null)return NotFound();
        await scope.RequireHerd(User,snap.HerdId);
        ViewBag.Report=r;ViewBag.Snapshot=snap;
        ViewBag.Content=JsonSerializer.Deserialize<AiContent>(r.ContentJson);
        return View();
    }
    [HttpPost]public Task<IActionResult> Approve(Guid id,int version)=>Save(()=>reports.Approve(User,id,version),"/Reports/Details/"+id);
}
