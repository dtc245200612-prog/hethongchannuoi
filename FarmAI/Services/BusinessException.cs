using System.Security.Claims;
using System.Text.Json;
using FarmAI.Data;
using FarmAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Services;

public class BusinessException(string message) : Exception(message) { }
public static class UserClaims
{
    public static Guid UserId(this ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
public class AccessScope(FarmDbContext db)
{
    public async Task<bool> CanBarn(ClaimsPrincipal user, Guid barnId)
    {
        if (user.IsInRole(Roles.Manager)) return true;
        return await db.Assignments.AnyAsync(a => a.UserId == user.UserId() && a.BarnId == barnId && a.ValidTo == null);
    }
    public async Task RequireBarn(ClaimsPrincipal user, Guid barnId)
    {
        if (!await CanBarn(user, barnId)) throw new BusinessException("Không có quyền trên chuồng này.");
    }
    public async Task<Herd> RequireHerd(ClaimsPrincipal user, Guid herdId)
    {
        var herd = await db.Herds.FindAsync(herdId) ?? throw new BusinessException("Không tìm thấy đàn.");
        await RequireBarn(user, herd.BarnId);
        return herd;
    }
    public IQueryable<Herd> VisibleHerds(ClaimsPrincipal user) => user.IsInRole(Roles.Manager)
        ? db.Herds.AsNoTracking()
        : db.Herds.AsNoTracking().Where(h => db.Assignments.Any(a => a.BarnId == h.BarnId && a.UserId == user.UserId() && a.ValidTo == null));
    public IQueryable<Barn> VisibleBarns(ClaimsPrincipal user) => user.IsInRole(Roles.Manager)
        ? db.Barns.AsNoTracking()
        : db.Barns.AsNoTracking().Where(b => db.Assignments.Any(a => a.BarnId == b.Id && a.UserId == user.UserId() && a.ValidTo == null));
}
public class FarmService(FarmDbContext db, AccessScope scope)
{
    private async Task Audit(Guid user, string action, string entity, Guid id, object? detail = null)
    {
        db.AuditLogs.Add(new AuditLog { ActorId = user, Action = action, EntityType = entity,
            EntityId = id, ChangesJson = JsonSerializer.Serialize(detail ?? new { }), RequestId = Guid.NewGuid().ToString("N") });
        await Task.CompletedTask;
    }
    private async Task RequireCapacity(Guid barnId, int additional, string species)
    {
        var barn = await db.Barns.FindAsync(barnId) ?? throw new BusinessException("Không tìm thấy chuồng.");
        if (barn.Status != "ACTIVE" || (barn.Species != "ALL" && !string.Equals(barn.Species, species, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException("Chuồng không hoạt động hoặc không phù hợp với loài.");
        var occupied = await db.Herds.Where(x => x.BarnId == barnId && x.Status == "ACTIVE").SumAsync(x => x.Quantity);
        if (additional > barn.Capacity - occupied) throw new BusinessException($"Chuồng {barn.Code} thiếu sức chứa ({occupied}/{barn.Capacity}).");
    }
    public async Task<Barn> CreateBarn(ClaimsPrincipal user, BarnForm f)
    {
        if (!user.IsInRole(Roles.Manager)) throw new BusinessException("Chỉ quản lý được thêm chuồng.");
        if (f.Capacity <= 0 || string.IsNullOrWhiteSpace(f.Code) || string.IsNullOrWhiteSpace(f.Name)) throw new BusinessException("Thông tin chuồng chưa hợp lệ.");
        if (await db.Barns.AnyAsync(x => x.Code == f.Code.Trim())) throw new BusinessException("Mã chuồng đã tồn tại.");
        var b = new Barn { Code = f.Code.Trim(), Name = f.Name.Trim(), Species = f.Species.Trim(), Capacity = f.Capacity };
        db.Barns.Add(b); await Audit(user.UserId(),"CREATE_BARN","Barn",b.Id);
        await db.SaveChangesAsync(); return b;
    }
    public async Task UpdateBarn(ClaimsPrincipal user, Guid id, int capacity, string status)
    {
        if (!user.IsInRole(Roles.Manager)) throw new BusinessException("Không có quyền.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var b = await db.Barns.FindAsync(id) ?? throw new BusinessException("Chuồng không tồn tại.");
        int occupied = await db.Herds.Where(h => h.BarnId == id && h.Status == "ACTIVE").SumAsync(h => h.Quantity);
        if (capacity < occupied || capacity <= 0) throw new BusinessException("Sức chứa mới thấp hơn số đang nuôi.");
        if (status is not ("ACTIVE" or "MAINTENANCE" or "ARCHIVED") || (status != "ACTIVE" && occupied > 0))
            throw new BusinessException("Không thể ngừng chuồng còn vật nuôi.");
        b.Capacity = capacity; b.Status = status;
        await Audit(user.UserId(),"UPDATE_BARN","Barn",id,new {capacity,status});
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task<Herd> CreateHerd(ClaimsPrincipal user, HerdForm f)
    {
        if (user.IsInRole(Roles.Employee)) throw new BusinessException("Nhân viên không được tạo đàn.");
        await scope.RequireBarn(user, f.BarnId);
        if (f.Quantity <= 0 || string.IsNullOrWhiteSpace(f.Code)) throw new BusinessException("Thông tin đàn chưa hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.Herds.AnyAsync(x => x.Code == f.Code.Trim())) throw new BusinessException("Mã đàn đã tồn tại.");
        await RequireCapacity(f.BarnId, f.Quantity, f.Species);
        var h = new Herd { Code=f.Code.Trim(),Name=f.Name.Trim(),Species=f.Species.Trim(),Breed=f.Breed,Purpose=f.Purpose,BarnId=f.BarnId,Quantity=f.Quantity };
        db.Herds.Add(h);
        db.Allocations.Add(new Allocation{ HerdId=h.Id, BarnId=f.BarnId });
        db.HerdMovements.Add(new HerdMovement{ HerdId=h.Id,Delta=f.Quantity,Kind="RECEIPT",SourceId=h.Id,Approved=true,ApprovedBy=user.UserId(),CreatedBy=user.UserId(),Reason="Tiếp nhận đàn" });
        await Audit(user.UserId(),"CREATE_HERD","Herd",h.Id,new {f.Quantity,f.BarnId});
        await db.SaveChangesAsync(); await tx.CommitAsync(); return h;
    }
    public async Task Transfer(ClaimsPrincipal user, TransferForm f)
    {
        if (user.IsInRole(Roles.Employee)) throw new BusinessException("Không có quyền chuyển đàn.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var h = await scope.RequireHerd(user,f.HerdId);
        await scope.RequireBarn(user,f.TargetBarnId);
        if (h.Version != f.Version) throw new BusinessException("Đàn đã được người khác thay đổi. Tải lại trang.");
        if (h.BarnId == f.TargetBarnId) throw new BusinessException("Đàn đã nằm tại chuồng này.");
        await RequireCapacity(f.TargetBarnId,h.Quantity,h.Species);
        var open = await db.Allocations.SingleOrDefaultAsync(x => x.HerdId == h.Id && x.UntilAt == null)
            ?? throw new BusinessException("Không có phân bổ chuồng hiện hành.");
        var now = DateTime.UtcNow; open.UntilAt=now;
        db.Allocations.Add(new Allocation{HerdId=h.Id,BarnId=f.TargetBarnId,FromAt=now});
        h.BarnId=f.TargetBarnId; h.Version++;
        await Audit(user.UserId(),"TRANSFER_BARN","Herd",h.Id,new {from=open.BarnId,to=f.TargetBarnId});
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task UpdateHerd(ClaimsPrincipal user, Guid id, string name, string? breed, string? purpose)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Không có quyền sửa đàn.");
        var h=await scope.RequireHerd(user,id);
        if(h.Status!="ACTIVE" || string.IsNullOrWhiteSpace(name))throw new BusinessException("Thông tin đàn không hợp lệ.");
        h.Name=name.Trim();h.Breed=breed?.Trim();h.Purpose=purpose?.Trim();h.Version++;
        await Audit(user.UserId(),"UPDATE_HERD","Herd",id,new{name,breed,purpose});
        await db.SaveChangesAsync();
    }
    public async Task ArchiveHerd(ClaimsPrincipal user, Guid id)
    {
        if(!user.IsInRole(Roles.Manager))throw new BusinessException("Chỉ quản lý có thể lưu trữ đàn.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var h=await scope.RequireHerd(user,id);
        if(h.Quantity!=0 || h.Status!="ACTIVE")throw new BusinessException("Chỉ lưu trữ đàn đã hết vật nuôi.");
        h.Status="ARCHIVED";h.Version++;
        var allocation=await db.Allocations.SingleOrDefaultAsync(a=>a.HerdId==id&&a.UntilAt==null);
        if(allocation!=null)allocation.UntilAt=DateTime.UtcNow;
        await Audit(user.UserId(),"ARCHIVE_HERD","Herd",id);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Split(ClaimsPrincipal user, SplitForm f)
    {
        if (user.IsInRole(Roles.Employee)) throw new BusinessException("Không có quyền chia đàn.");
        if (f.Quantity<=0 || f.SourceHerdId==f.TargetHerdId) throw new BusinessException("Lượng chia hoặc đàn đích không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var source=await scope.RequireHerd(user,f.SourceHerdId); var target=await scope.RequireHerd(user,f.TargetHerdId);
        if (source.Species!=target.Species || source.Quantity<f.Quantity || source.Status!="ACTIVE" || target.Status!="ACTIVE")
            throw new BusinessException("Không thể chia đàn: loài, trạng thái hoặc số lượng không hợp lệ.");
        if (source.BarnId!=target.BarnId) await RequireCapacity(target.BarnId,f.Quantity,target.Species);
        source.Quantity=FarmRules.Remaining(source.Quantity,-f.Quantity);
        target.Quantity=FarmRules.Remaining(target.Quantity,f.Quantity);
        source.Version++;target.Version++;
        var key=Guid.NewGuid();
        db.HerdMovements.Add(new HerdMovement{HerdId=source.Id,Delta=-f.Quantity,Kind="SPLIT_OUT",SourceId=key,CreatedBy=user.UserId(),ApprovedBy=user.UserId(),Approved=true,Reason="Chia đàn"});
        db.HerdMovements.Add(new HerdMovement{HerdId=target.Id,Delta=f.Quantity,Kind="SPLIT_IN",SourceId=key,CreatedBy=user.UserId(),ApprovedBy=user.UserId(),Approved=true,Reason="Nhập từ đàn nguồn"});
        await Audit(user.UserId(),"SPLIT_HERD","Herd",source.Id,new {target.Id,f.Quantity});
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task RequestMovement(ClaimsPrincipal user, MovementForm f)
    {
        var herd=await scope.RequireHerd(user,f.HerdId);
        if (f.Delta==0 || string.IsNullOrWhiteSpace(f.Reason) || f.Kind is not ("DEATH" or "ADJUSTMENT")) throw new BusinessException("Phải có biến động khác 0 và lý do.");
        if (f.Kind=="DEATH" && f.Delta>=0) throw new BusinessException("Hao hụt phải là số giảm.");
        if (f.RequestKey==Guid.Empty) throw new BusinessException("Thiếu khóa yêu cầu.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.HerdMovements.AnyAsync(m=>m.SourceId==f.RequestKey && m.Kind==f.Kind && m.HerdId==f.HerdId)) return;
        bool approved=user.IsInRole(Roles.Manager);
        if(approved)
        {
            if(f.Delta>0) await RequireCapacity(herd.BarnId,f.Delta,herd.Species);
            herd.Quantity=FarmRules.Remaining(herd.Quantity,f.Delta);herd.Version++;
        }
        var m=new HerdMovement{HerdId=f.HerdId,Delta=f.Delta,Kind=f.Kind,SourceId=f.RequestKey,Reason=f.Reason,CreatedBy=user.UserId(),Approved=approved,ApprovedBy=approved?user.UserId():null};
        db.HerdMovements.Add(m);await Audit(user.UserId(),"REQUEST_MOVEMENT","HerdMovement",m.Id,new {f.Delta,f.Kind,approved});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task ApproveMovement(ClaimsPrincipal user, Guid id)
    {
        if(!user.IsInRole(Roles.Manager)) throw new BusinessException("Chỉ quản lý được duyệt biến động.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var m=await db.HerdMovements.FindAsync(id) ?? throw new BusinessException("Không tìm thấy yêu cầu.");
        if(m.Approved) return;
        var herd=await db.Herds.FindAsync(m.HerdId) ?? throw new BusinessException("Đàn không tồn tại.");
        if(m.Delta>0) await RequireCapacity(herd.BarnId,m.Delta,herd.Species);
        herd.Quantity=FarmRules.Remaining(herd.Quantity,m.Delta);herd.Version++;
        m.Approved=true;m.ApprovedBy=user.UserId();
        await Audit(user.UserId(),"APPROVE_MOVEMENT","HerdMovement",id);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<CareLog> CreateCare(ClaimsPrincipal user,CareForm f)
    {
        await scope.RequireHerd(user,f.HerdId);
        if(f.Quantity<=0 || f.UnitPrice<0 || f.Kind is not ("FEED" or "MEDICINE" or "OBSERVATION") || string.IsNullOrWhiteSpace(f.ItemName))
            throw new BusinessException("Thông tin chăm sóc không hợp lệ.");
        if(f.Kind=="MEDICINE" && string.IsNullOrWhiteSpace(f.Reference))throw new BusinessException("Nhật ký thuốc phải có căn cứ chuyên môn.");
        var l=new CareLog{HerdId=f.HerdId,CreatedBy=user.UserId(),Kind=f.Kind,ItemName=f.ItemName,Unit=f.Unit,Quantity=f.Quantity,UnitPrice=f.UnitPrice,Reference=f.Reference,Note=f.Note};
        db.CareLogs.Add(l);await Audit(user.UserId(),"CARE_CREATED","CareLog",l.Id);
        await db.SaveChangesAsync();return l;
    }
    public async Task ApproveCare(ClaimsPrincipal user,Guid id)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Không có quyền duyệt nhật ký.");
        var l=await db.CareLogs.FindAsync(id)??throw new BusinessException("Không có nhật ký.");
        await scope.RequireHerd(user,l.HerdId);
        if(l.Status=="APPROVED")return;
        if(l.CreatedBy==user.UserId()) throw new BusinessException("Người nhập không tự duyệt nhật ký của mình.");
        l.Status="APPROVED";l.ApprovedBy=user.UserId();
        await Audit(user.UserId(),"CARE_APPROVED","CareLog",id);
        await db.SaveChangesAsync();
    }
    public async Task AddGrowth(ClaimsPrincipal user,GrowthForm f)
    {
        await scope.RequireHerd(user,f.HerdId);
        if(f.SampleSize<=0 || f.WeightKg<=0 || f.MeasuredAt>DateTime.UtcNow.AddDays(1))throw new BusinessException("Mẫu cân không hợp lệ.");
        var g=new GrowthRecord{HerdId=f.HerdId,SampleSize=f.SampleSize,WeightKg=f.WeightKg,Method=f.Method,MeasuredAt=DateTime.SpecifyKind(f.MeasuredAt,DateTimeKind.Utc),CreatedBy=user.UserId()};
        db.GrowthRecords.Add(g);await Audit(user.UserId(),"ADD_GROWTH","GrowthRecord",g.Id);
        await db.SaveChangesAsync();
    }
    public async Task<Animal> AddAnimal(ClaimsPrincipal user,AnimalForm f)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Không có quyền quản lý cá thể.");
        await scope.RequireHerd(user,f.HerdId);
        if(await db.Animals.AnyAsync(x=>x.Tag==f.Tag))throw new BusinessException("Mã tai đã tồn tại.");
        var a=new Animal{HerdId=f.HerdId,Tag=f.Tag,Sex=f.Sex};db.Animals.Add(a);
        await Audit(user.UserId(),"CREATE_ANIMAL","Animal",a.Id);await db.SaveChangesAsync();return a;
    }
    public async Task RegisterBirth(ClaimsPrincipal user,ReproductionForm f)
    {
        if(user.IsInRole(Roles.Employee))throw new BusinessException("Chỉ kỹ thuật viên/quản lý ghi sinh sản.");
        if(f.LiveBorn<0 || f.StillBorn<0 || f.Accepted<0 || f.Accepted>f.LiveBorn || f.RequestKey==Guid.Empty)
            throw new BusinessException("Số sinh sống và tiếp nhận không hợp lệ.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if(await db.ReproductionEvents.AnyAsync(x=>x.IdempotencyKey==f.RequestKey))return;
        var a=await db.Animals.FindAsync(f.MotherId)??throw new BusinessException("Không có cá thể mẹ.");
        if(a.Sex!="FEMALE")throw new BusinessException("Cá thể mẹ không hợp lệ.");
        await scope.RequireHerd(user,a.HerdId);
        var h=await scope.RequireHerd(user,f.TargetHerdId);
        if(f.Accepted>0)await RequireCapacity(h.BarnId,f.Accepted,h.Species);
        var e=new ReproductionEvent{MotherId=f.MotherId,TargetHerdId=h.Id,LiveBorn=f.LiveBorn,StillBorn=f.StillBorn,Accepted=f.Accepted,EventDate=DateOnly.FromDateTime(DateTime.Today),CreatedBy=user.UserId(),IdempotencyKey=f.RequestKey};
        db.ReproductionEvents.Add(e);
        if(f.Accepted>0){h.Quantity=FarmRules.Remaining(h.Quantity,f.Accepted);h.Version++;db.HerdMovements.Add(new HerdMovement{HerdId=h.Id,Kind="BIRTH",SourceId=e.Id,Delta=f.Accepted,Reason="Sinh sống tiếp nhận",CreatedBy=user.UserId(),Approved=true,ApprovedBy=user.UserId()});}
        await Audit(user.UserId(),"REGISTER_BIRTH","ReproductionEvent",e.Id);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
