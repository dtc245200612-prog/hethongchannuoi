using FarmAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Barn> Barns => Set<Barn>();
    public DbSet<Herd> Herds => Set<Herd>();
    public DbSet<CareLog> CareLogs => Set<CareLog>();
    public DbSet<CareSchedule> CareSchedules => Set<CareSchedule>();
    public DbSet<VaccineRecord> Vaccines => Set<VaccineRecord>();
    public DbSet<GrowthRecord> GrowthRecords => Set<GrowthRecord>();
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();
    public DbSet<FeedTransaction> FeedTransactions => Set<FeedTransaction>();
    public DbSet<MedicineItem> MedicineItems => Set<MedicineItem>();
    public DbSet<ReproductionRecord> ReproductionRecords => Set<ReproductionRecord>();
    public DbSet<SaleRecord> Sales => Set<SaleRecord>();
    public DbSet<ExpenseRecord> Expenses => Set<ExpenseRecord>();
    public DbSet<AiLog> AiLogs => Set<AiLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Barn>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Herd>().HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<Herd>()
            .HasOne(x => x.Barn)
            .WithMany(x => x.Herds)
            .HasForeignKey(x => x.BarnId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareLog>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.CareLogs)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CareSchedule>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.CareSchedules)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VaccineRecord>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.Vaccines)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GrowthRecord>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.GrowthRecords)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReproductionRecord>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.ReproductionRecords)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleRecord>()
            .HasOne(x => x.Herd)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeedTransaction>()
            .HasOne(x => x.FeedItem)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.FeedItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeedTransaction>()
            .HasOne(x => x.Herd)
            .WithMany()
            .HasForeignKey(x => x.HerdId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AiLog>()
            .HasOne(x => x.AppUser)
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);

        base.OnModelCreating(modelBuilder);
    }
}
