using FarmAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Data;

public class FarmDbContext(DbContextOptions<FarmDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LoginSession> Sessions => Set<LoginSession>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Barn> Barns => Set<Barn>();
    public DbSet<Herd> Herds => Set<Herd>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<HerdMovement> HerdMovements => Set<HerdMovement>();
    public DbSet<CareLog> CareLogs => Set<CareLog>();
    public DbSet<GrowthRecord> GrowthRecords => Set<GrowthRecord>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<ReproductionEvent> ReproductionEvents => Set<ReproductionEvent>();
    public DbSet<Vaccine> Vaccines => Set<Vaccine>();
    public DbSet<VaccinationPlan> VaccinationPlans => Set<VaccinationPlan>();
    public DbSet<VaccinationEvent> VaccinationEvents => Set<VaccinationEvent>();
    public DbSet<FarmNotification> Notifications => Set<FarmNotification>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FarmSnapshot> Snapshots => Set<FarmSnapshot>();
    public DbSet<AiReport> AiReports => Set<AiReport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        m.Entity<Assignment>().HasKey(x => new { x.UserId, x.BarnId });
        m.Entity<Barn>().HasIndex(x => x.Code).IsUnique();
        m.Entity<Herd>().HasIndex(x => x.Code).IsUnique();
        m.Entity<Herd>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<Allocation>().HasIndex(x => new { x.HerdId, x.UntilAt }).IsUnique()
            .HasFilter("[UntilAt] IS NULL"); // Sqlite provider: demo creates filtered index via provider conventions.
        m.Entity<HerdMovement>().HasIndex(x => new { x.Kind, x.SourceId, x.HerdId }).IsUnique();
        m.Entity<Animal>().HasIndex(x => x.Tag).IsUnique();
        m.Entity<ReproductionEvent>().HasIndex(x => x.IdempotencyKey).IsUnique();
        m.Entity<VaccinationEvent>().HasIndex(x => x.IdempotencyKey).IsUnique();
        m.Entity<FarmNotification>().HasIndex(x => new { x.PlanId, x.RecipientId, x.Kind, x.RemindDate }).IsUnique();
        m.Entity<Sale>().HasIndex(x => x.ConfirmKey).IsUnique();
        m.Entity<Sale>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Sale>().HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<CareLog>().Ignore(x => x.Cost);
        m.Entity<GrowthRecord>().Ignore(x => x.AverageKg);
        m.Entity<SaleLine>().Ignore(x => x.Amount);
        m.Entity<CareLog>().Property(x => x.Quantity).HasPrecision(14, 3);
        m.Entity<CareLog>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        m.Entity<GrowthRecord>().Property(x => x.WeightKg).HasPrecision(14, 3);
        m.Entity<SaleLine>().Property(x => x.WeightKg).HasPrecision(14, 3);
        m.Entity<SaleLine>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        m.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        m.Entity<CareLog>().HasIndex(x => new { x.HerdId, x.OccurredAt });
        m.Entity<GrowthRecord>().HasIndex(x => new { x.HerdId, x.MeasuredAt });
        m.Entity<VaccinationPlan>().HasIndex(x => x.PlannedDate);
        // Explicit RESTRICT FKs prevent deletion/cascade of confirmed business evidence.
        m.Entity<LoginSession>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Assignment>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Assignment>().HasOne<Barn>().WithMany().HasForeignKey(x => x.BarnId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Herd>().HasOne(x => x.Barn).WithMany().HasForeignKey(x => x.BarnId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Allocation>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Allocation>().HasOne<Barn>().WithMany().HasForeignKey(x => x.BarnId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<HerdMovement>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<HerdMovement>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        m.Entity<CareLog>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<GrowthRecord>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Animal>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ReproductionEvent>().HasOne<Animal>().WithMany().HasForeignKey(x => x.MotherId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ReproductionEvent>().HasOne<Herd>().WithMany().HasForeignKey(x => x.TargetHerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<VaccinationPlan>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<VaccinationPlan>().HasOne<Vaccine>().WithMany().HasForeignKey(x => x.VaccineId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<VaccinationEvent>().HasOne<VaccinationPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<FarmNotification>().HasOne<VaccinationPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<FarmNotification>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<SaleLine>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<FarmSnapshot>().HasOne<Herd>().WithMany().HasForeignKey(x => x.HerdId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<AiReport>().HasOne<FarmSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}
