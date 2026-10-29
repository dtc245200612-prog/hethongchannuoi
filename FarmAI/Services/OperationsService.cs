using System.Data;
using System.Security.Claims;
using FarmAI.Data;
using FarmAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Services;

public class OperationsService(FarmDbContext db, AccessScope scope)
{
    private void Audit(ClaimsPrincipal user,string action,string entity,Guid id,object? detail=null)
    {
        db.AuditLogs.Add(new AuditLog{ActorId=user.UserId(),Action=action,EntityType=entity,EntityId=id,
            ChangesJson=System.Text.Json.JsonSerializer.Serialize(detail??new{}),RequestId=Guid.NewGuid().ToString("N")});
    }
    public async Task CreatePlan(ClaimsPrincipal user,PlanForm f)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Chỉ kỹ thuật viên hoặc quản lý được lập lịch.");
        var h=await scope.RequireHerd(user,f.HerdId);
        var v=await db.Vaccines.FindAsync(f.VaccineId);
        if(v?.Active!=true || f.Target<=0 || f.Target>h.Quantity || string.IsNullOrWhiteSpace(f.Reference))throw new BusinessException("Lịch tiêm không hợp lệ hoặc vượt số lượng đàn.");
        if(!await db.Users.AnyAsync(u=>u.Id==f.AssignedTo && u.Active))throw new BusinessException("Người phụ trách không hợp lệ.");
        var p=new VaccinationPlan{HerdId=f.HerdId,VaccineId=f.VaccineId,PlannedDate=f.PlannedDate,Target=f.Target,Reference=f.Reference,AssignedTo=f.AssignedTo};
        db.VaccinationPlans.Add(p);Audit(user,"PLAN_CREATED","VaccinationPlan",p.Id);
        await db.SaveChangesAsync();
    }
    public async Task AdjustPlan(ClaimsPrincipal user,Guid id,DateOnly? newDate,string? reason,bool cancel=false)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Không có quyền sửa lịch.");
        var p=await db.VaccinationPlans.FindAsync(id)??throw new BusinessException("Lịch không tồn tại.");
        await scope.RequireHerd(user,p.HerdId);
        if(string.IsNullOrWhiteSpace(reason) || p.Status is "COMPLETED" or "CANCELLED")throw new BusinessException("Cần lý do và lịch chưa đóng.");
        if(!cancel && newDate==null)throw new BusinessException("Chọn ngày mới khi dời lịch.");
        var old=p.PlannedDate;
        if(cancel)p.Status="CANCELLED";else if(newDate.HasValue)p.PlannedDate=newDate.Value;
        p.Version++;
        Audit(user,"ADJUST_PLAN","VaccinationPlan",id,new{old,newDate,cancel,reason});
        await db.SaveChangesAsync();
    }
    public async Task SubmitInjection(ClaimsPrincipal user,VaccinationEventForm f)
    {
        if(f.Quantity<=0 || string.IsNullOrWhiteSpace(f.Lot) || f.RequestKey==Guid.Empty)throw new BusinessException("Thiếu số con, số lô hoặc khóa thao tác.");
        var p=await db.VaccinationPlans.FindAsync(f.PlanId)??throw new BusinessException("Lịch không tồn tại.");
        await scope.RequireHerd(user,p.HerdId);
        if(p.Status is "COMPLETED" or "CANCELLED")throw new BusinessException("Lịch đã đóng.");
        if(await db.VaccinationEvents.AnyAsync(x=>x.IdempotencyKey==f.RequestKey))return;
        var e=new VaccinationEvent{PlanId=f.PlanId,Quantity=f.Quantity,Lot=f.Lot,PerformedBy=user.UserId(),IdempotencyKey=f.RequestKey};
        db.VaccinationEvents.Add(e);Audit(user,"INJECTION_SUBMITTED","VaccinationEvent",e.Id);
        await db.SaveChangesAsync();
    }
    public async Task ApproveInjection(ClaimsPrincipal user,Guid eventId)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Chỉ kỹ thuật viên/quản lý được xác nhận tiêm.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var e=await db.VaccinationEvents.FindAsync(eventId)??throw new BusinessException("Không tìm thấy lần tiêm.");
        var p=await db.VaccinationPlans.FindAsync(e.PlanId)??throw new BusinessException("Lịch không tồn tại.");
        await scope.RequireHerd(user,p.HerdId);
        if(e.ApprovedBy!=null)return;
        if(p.Status is "CANCELLED" or "COMPLETED")throw new BusinessException("Lịch đã đóng.");
        if(e.PerformedBy==user.UserId())throw new BusinessException("Người thực hiện không tự phê duyệt.");
        var total=await db.VaccinationEvents.Where(x=>x.PlanId==e.PlanId && x.ApprovedBy!=null).SumAsync(x=>x.Quantity);
        if(total+e.Quantity>p.Target)throw new BusinessException("Số đã tiêm vượt mục tiêu.");
        e.ApprovedBy=user.UserId();p.Status=total+e.Quantity==p.Target?"COMPLETED":"PARTIAL";p.Version++;
        Audit(user,"INJECTION_APPROVED","VaccinationEvent",e.Id,new{e.Quantity,p.Status});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<int> RefreshAlerts()
    {
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")));
        var due=await db.VaccinationPlans.Where(p=>p.Status=="PLANNED"||p.Status=="PARTIAL").ToListAsync();
        int created=0;
        foreach(var p in due)
        {
            var diff=p.PlannedDate.DayNumber-today.DayNumber;
            if(diff>3)continue;
            string kind=diff<0?"OVERDUE":diff==0?"DUE":"UPCOMING";
            if(await db.Notifications.AnyAsync(n=>n.PlanId==p.Id && n.RecipientId==p.AssignedTo && n.Kind==kind && n.RemindDate==today))continue;
            var completed=await db.VaccinationEvents.Where(e=>e.PlanId==p.Id && e.ApprovedBy!=null).SumAsync(e=>e.Quantity);
            db.Notifications.Add(new FarmNotification{PlanId=p.Id,RecipientId=p.AssignedTo,RemindDate=today,Kind=kind,
                Message=$"Lịch tiêm {p.PlannedDate:dd/MM/yyyy}: còn {Math.Max(0,p.Target-completed)} con. {kind}."});created++;
        }
        if(created>0)await db.SaveChangesAsync();return created;
    }
    public async Task<Sale> CreateSale(ClaimsPrincipal user,SaleForm f)
    {
        if(!user.IsInRole(Roles.Manager))throw new BusinessException("Chỉ quản lý được lập phiếu bán.");
        await scope.RequireHerd(user,f.HerdId);
        if(string.IsNullOrWhiteSpace(f.CustomerName))throw new BusinessException("Thiếu tên khách.");
        _=FarmRules.LineTotal(f.Basis,f.Quantity,f.WeightKg,f.UnitPrice);
        var s=new Sale{CustomerName=f.CustomerName,CreatedBy=user.UserId()};
        s.Lines.Add(new SaleLine{HerdId=f.HerdId,Quantity=f.Quantity,WeightKg=f.WeightKg,Basis=f.Basis,UnitPrice=f.UnitPrice,SaleId=s.Id});
        db.Sales.Add(s);Audit(user,"SALE_DRAFT","Sale",s.Id);
        await db.SaveChangesAsync();return s;
    }
    public async Task AddSaleLine(ClaimsPrincipal user,LineForm f)
    {
        if(!user.IsInRole(Roles.Manager))throw new BusinessException("Không có quyền.");
        var s=await db.Sales.FindAsync(f.SaleId)??throw new BusinessException("Không có phiếu bán.");
        if(s.Status!="DRAFT")throw new BusinessException("Phiếu đã xác nhận không được sửa.");
        await scope.RequireHerd(user,f.HerdId);
        _=FarmRules.LineTotal(f.Basis,f.Quantity,f.WeightKg,f.UnitPrice);
        var l=new SaleLine{SaleId=s.Id,HerdId=f.HerdId,Quantity=f.Quantity,WeightKg=f.WeightKg,Basis=f.Basis,UnitPrice=f.UnitPrice};
        db.SaleLines.Add(l);Audit(user,"SALE_LINE_ADDED","SaleLine",l.Id);
        await db.SaveChangesAsync();
    }
    public async Task ConfirmSale(ClaimsPrincipal user,Guid id,Guid key)
    {
        if(!user.IsInRole(Roles.Manager))throw new BusinessException("Chỉ quản lý được xác nhận phiếu bán.");
        if(key==Guid.Empty)throw new BusinessException("Thiếu khóa xác nhận.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var s=await db.Sales.Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không có phiếu.");
        if(s.Status=="CONFIRMED")
        {
            if(s.ConfirmKey==key)return;
            throw new BusinessException("Phiếu đã xác nhận; không thể xác nhận lần nữa bằng khóa khác.");
        }
        if(s.Status!="DRAFT" || s.Lines.Count==0)throw new BusinessException("Phiếu nháp trống hoặc không hợp lệ.");
        // Aggregate quantities by herd to prevent oversell with repeated lines.
        var grouped=s.Lines.GroupBy(l=>l.HerdId).Select(g=>new { HerdId=g.Key, Quantity=g.Sum(x=>x.Quantity)}).OrderBy(x=>x.HerdId).ToList();
        foreach(var x in grouped)
        {
            var h=await db.Herds.FindAsync(x.HerdId)??throw new BusinessException("Đàn trên phiếu không tồn tại.");
            if(h.Status!="ACTIVE" || h.Quantity<x.Quantity)throw new BusinessException($"Đàn {h.Code} không đủ số lượng để bán.");
            await scope.RequireHerd(user,h.Id);
            // Farm management must check veterinary withdrawal outside this educational prototype.
            h.Quantity=FarmRules.Remaining(h.Quantity,-x.Quantity);h.Version++;
            db.HerdMovements.Add(new HerdMovement{HerdId=h.Id,Delta=-x.Quantity,Kind="SALE",SourceId=s.Id,Reason="Xác nhận phiếu bán",CreatedBy=user.UserId(),ApprovedBy=user.UserId(),Approved=true});
        }
        s.Status="CONFIRMED";s.SoldAt=DateTime.UtcNow;s.ConfirmedBy=user.UserId();s.ConfirmKey=key;
        s.PayloadHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            string.Join("|",s.Lines.OrderBy(x=>x.Id).Select(x=>$"{x.HerdId}:{x.Quantity}:{x.Basis}:{x.UnitPrice}:{x.WeightKg}")))));
        Audit(user,"SALE_CONFIRMED","Sale",s.Id,new{key,total=s.Lines.Sum(x=>x.Amount)});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task MakePayment(ClaimsPrincipal user,PaymentForm f)
    {
        if(!user.IsInRole(Roles.Manager))throw new BusinessException("Chỉ quản lý được ghi nhận thanh toán.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var s=await db.Sales.Include(x=>x.Lines).Include(x=>x.Payments).FirstOrDefaultAsync(x=>x.Id==f.SaleId)
            ??throw new BusinessException("Không có phiếu.");
        if(s.Status!="CONFIRMED" || f.Amount<=0)throw new BusinessException("Phiếu chưa xác nhận hoặc số tiền không hợp lệ.");
        var balance=s.Lines.Sum(x=>x.Amount)-s.Payments.Sum(x=>x.Amount);
        if(f.Amount>balance)throw new BusinessException($"Số tiền vượt dư nợ còn {balance:N0} VND.");
        var p=new Payment{SaleId=s.Id,Amount=f.Amount,Method=f.Method,Reference=f.Reference};
        db.Payments.Add(p);Audit(user,"PAYMENT","Payment",p.Id,new{f.Amount});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
