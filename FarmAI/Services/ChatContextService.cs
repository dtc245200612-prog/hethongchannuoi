using System.Security.Claims;
using System.Text.Json;
using FarmAI.Data;
using FarmAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Services;

public record ChatFarmContext(string Json, List<ChatSource> Sources, int HerdCount, int AnimalCount, int OpenPlans);

// Only server-side, role-scoped and bounded DB projections are sent to the AI provider.
public class ChatContextService(FarmDbContext db, AccessScope scope)
{
    public async Task<ChatFarmContext> Build(ClaimsPrincipal user, CancellationToken ct)
    {
        var herdIds=scope.VisibleHerds(user).Where(x=>x.Status=="ACTIVE").Select(x=>x.Id);
        var aggregates=await scope.VisibleHerds(user).Where(x=>x.Status=="ACTIVE")
            .GroupBy(x=>1).Select(g=>new{Herds=g.Count(),Animals=g.Sum(x=>x.Quantity)})
            .FirstOrDefaultAsync(ct);
        var herds=await scope.VisibleHerds(user).Where(x=>x.Status=="ACTIVE")
            .OrderBy(x=>x.Code).Take(25)
            .Select(x=>new{x.Id,x.Code,x.Name,x.Species,x.Quantity,x.BarnId}).ToListAsync(ct);
        var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var plans=await db.VaccinationPlans.AsNoTracking()
            .Where(p=>herdIds.Contains(p.HerdId) && (p.Status=="PLANNED"||p.Status=="PARTIAL"))
            .OrderBy(p=>p.PlannedDate).ThenBy(p=>p.Id).Take(20)
            .Select(p=>new{p.Id,p.HerdId,p.PlannedDate,p.Target,p.Status,p.VaccineId}).ToListAsync(ct);
        var planIds=plans.Select(p=>p.Id).ToList();
        var confirmedVaccinations=await db.VaccinationEvents.AsNoTracking()
            .Where(e=>planIds.Contains(e.PlanId) && e.ApprovedBy!=null)
            .GroupBy(e=>e.PlanId).Select(g=>new{PlanId=g.Key,Done=g.Sum(e=>e.Quantity)})
            .ToDictionaryAsync(x=>x.PlanId,x=>x.Done,ct);
        var openPlans=await db.VaccinationPlans.AsNoTracking()
            .CountAsync(p=>herdIds.Contains(p.HerdId) && (p.Status=="PLANNED"||p.Status=="PARTIAL"),ct);
        var vaccineIds=plans.Select(p=>p.VaccineId).Distinct().ToList();
        var vaccineNames=await db.Vaccines.AsNoTracking().Where(v=>vaccineIds.Contains(v.Id))
            .ToDictionaryAsync(v=>v.Id,v=>v.Name,ct);
        var growth=await db.GrowthRecords.AsNoTracking().Where(x=>herdIds.Contains(x.HerdId))
            .OrderByDescending(x=>x.MeasuredAt).ThenBy(x=>x.Id).Take(18)
            .Select(x=>new{x.Id,x.HerdId,x.MeasuredAt,x.SampleSize,x.WeightKg,x.Method}).ToListAsync(ct);
        var care=await db.CareLogs.AsNoTracking().Where(x=>herdIds.Contains(x.HerdId)&&x.Status=="APPROVED")
            .OrderByDescending(x=>x.OccurredAt).ThenBy(x=>x.Id).Take(18)
            .Select(x=>new{x.Id,x.HerdId,x.Kind,x.ItemName,x.Quantity,x.Unit,x.OccurredAt}).ToListAsync(ct);
        var recentComparisons=growth.GroupBy(g=>g.HerdId).Select(g=>g.OrderByDescending(x=>x.MeasuredAt).Take(2).ToList())
            .Where(items=>items.Count==2).Select(items=>
            {
                var newer=items[0];var older=items[1];
                var comparable=newer.Method==older.Method && newer.MeasuredAt>older.MeasuredAt &&
                    newer.SampleSize>0 && older.SampleSize>0;
                decimal? adg=comparable?
                    Math.Round((newer.WeightKg/newer.SampleSize-older.WeightKg/older.SampleSize) /
                        (decimal)(newer.MeasuredAt-older.MeasuredAt).TotalDays,3):(decimal?)null;
                return new {herd_source_id="HERD:"+newer.HerdId.ToString("N"),
                    older_source_id="GROWTH:"+older.Id.ToString("N"),newer_source_id="GROWTH:"+newer.Id.ToString("N"),
                    adg_kg_per_day=adg,comparison_valid=comparable,
                    caution="ADG từ mẫu cân, không chứng minh tăng trưởng từng cá thể; cần rà soát biến động thành phần đàn."};
            }).Take(8).ToList();
        var overduePlans=plans.Count(p=>p.PlannedDate<today);
        var sources=new List<ChatSource>{new("SUMMARY","Tổng hợp phạm vi của tài khoản","/Home")};
        sources.AddRange(herds.Select(x=>new ChatSource("HERD:"+x.Id.ToString("N"),$"Đàn {x.Code}","/Herds/Details/"+x.Id)));
        sources.AddRange(plans.Select(x=>new ChatSource("PLAN:"+x.Id.ToString("N"),$"Lịch tiêm {x.PlannedDate:dd/MM/yyyy}","/Vaccinations")));
        sources.AddRange(growth.Select(x=>new ChatSource("GROWTH:"+x.Id.ToString("N"),"Lần cân "+x.MeasuredAt.ToString("dd/MM/yyyy"),"/Growth")));
        sources.AddRange(care.Select(x=>new ChatSource("CARE:"+x.Id.ToString("N"),"Nhật ký chăm sóc "+x.OccurredAt.ToString("dd/MM/yyyy"),"/Care")));
        var json=JsonSerializer.Serialize(new {
            measured_at_utc=DateTime.UtcNow,
            local_today=today.ToString("yyyy-MM-dd"),
            current_user_role=user.IsInRole(Roles.Manager)?Roles.Manager:(user.IsInRole(Roles.Technician)?Roles.Technician:Roles.Employee),
            coverage="Chỉ đàn/chuồng có quyền xem. Danh sách chi tiết giới hạn 25 đàn, 20 lịch tiêm, 18 lượt cân, 18 nhật ký; tổng tính toàn phạm vi. Không tính số quá hạn trên toàn bộ kế hoạch từ mẫu 20 lịch. ADG mẫu cân chưa xét biến động thành phần đàn.",
            totals=new{herd_count=aggregates?.Herds??0,animal_count=aggregates?.Animals??0,open_vaccination_plans=openPlans,overdue_in_sample=overduePlans},
            herds=herds.Select(h=>new{source_id="HERD:"+h.Id.ToString("N"),h.Code,h.Name,h.Species,h.Quantity}),
            vaccination_plans=plans.Select(p=>new{source_id="PLAN:"+p.Id.ToString("N"),
                herd_source_id="HERD:"+p.HerdId.ToString("N"),p.PlannedDate,p.Target,p.Status,
                vaccine=vaccineNames.GetValueOrDefault(p.VaccineId,"Không rõ"),overdue=p.PlannedDate<today,
                approved_quantity=confirmedVaccinations.GetValueOrDefault(p.Id),remaining=p.Target-confirmedVaccinations.GetValueOrDefault(p.Id)}),
            growth_records=growth.Select(g=>new{source_id="GROWTH:"+g.Id.ToString("N"),
                herd_source_id="HERD:"+g.HerdId.ToString("N"),g.MeasuredAt,g.SampleSize,g.WeightKg,
                average_kg=g.SampleSize>0?(decimal?)(g.WeightKg/g.SampleSize):null,g.Method}),
            recent_growth_comparisons=recentComparisons,
            approved_care_logs=care.Select(c=>new{source_id="CARE:"+c.Id.ToString("N"),
                herd_source_id="HERD:"+c.HerdId.ToString("N"),c.Kind,c.ItemName,c.Quantity,c.Unit,c.OccurredAt}),
            source_ids=sources.Select(s=>s.Id)
        });
        if(json.Length>48000)throw new ChatFailure(413,"CONTEXT_TOO_LARGE","Dữ liệu vượt giới hạn hội thoại.");
        return new(json,sources,aggregates?.Herds??0,aggregates?.Animals??0,openPlans);
    }
}
