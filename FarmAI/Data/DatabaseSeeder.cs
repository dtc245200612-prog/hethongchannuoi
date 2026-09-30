using FarmAI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmAI.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<AppUser>();
            var users = new[]
            {
                new AppUser { Username = "admin", FullName = "Quản lý hệ thống", Role = RoleNames.Manager },
                new AppUser { Username = "ktv", FullName = "Kỹ thuật viên trang trại", Role = RoleNames.Technician },
                new AppUser { Username = "nv", FullName = "Nhân viên chăm sóc", Role = RoleNames.Employee }
            };
            var passwords = new[] { "admin123", "ktv123", "nv123" };
            for (var i = 0; i < users.Length; i++)
            {
                users[i].PasswordHash = hasher.HashPassword(users[i], passwords[i]);
            }
            db.Users.AddRange(users);
            await db.SaveChangesAsync();
        }

        if (await db.Barns.AnyAsync()) return;

        var barns = new[]
        {
            new Barn { Code = "CH-BO-01", Name = "Chuồng bò sữa A", Capacity = 260, Temperature = 26.5m, Humidity = 70, Status = "Tốt" },
            new Barn { Code = "CH-LON-01", Name = "Chuồng lợn thịt B", Capacity = 300, Temperature = 27.0m, Humidity = 68, Status = "Cần theo dõi" },
            new Barn { Code = "CH-GA-01", Name = "Khu gà đẻ C", Capacity = 260, Temperature = 25.0m, Humidity = 64, Status = "Tốt" }
        };
        db.Barns.AddRange(barns);
        await db.SaveChangesAsync();

        var herds = new[]
        {
            new Herd { Code = "DAN-BO-001", Name = "Bò sữa HF 01", Species = "Bò", Breed = "Holstein Friesian", BarnId = barns[0].Id, Quantity = 220, StartDate = DateTime.Today.AddMonths(-9), Status = "Ổn định", MortalityCount = 1 },
            new Herd { Code = "DAN-LON-002", Name = "Lợn thịt 02", Species = "Lợn", Breed = "Duroc × Yorkshire", BarnId = barns[1].Id, Quantity = 238, StartDate = DateTime.Today.AddMonths(-4), Status = "Cần theo dõi", MortalityCount = 3 },
            new Herd { Code = "DAN-GA-003", Name = "Gà đẻ 03", Species = "Gà", Breed = "Isa Brown", BarnId = barns[2].Id, Quantity = 200, StartDate = DateTime.Today.AddMonths(-6), Status = "Ổn định", MortalityCount = 2 }
        };
        db.Herds.AddRange(herds);
        await db.SaveChangesAsync();

        db.CareLogs.AddRange(
            new CareLog { HerdId = herds[0].Id, Date = DateTime.Today, Type = "Chăm sóc", Description = "Vệ sinh máng uống và kiểm tra khẩu phần.", StaffName = "Nhân viên chăm sóc" },
            new CareLog { HerdId = herds[1].Id, Date = DateTime.Today.AddDays(-1), Type = "Sức khỏe", Description = "Theo dõi biểu hiện ăn kém ở một số cá thể.", StaffName = "Kỹ thuật viên trang trại", RequiresHealthCheck = true },
            new CareLog { HerdId = herds[2].Id, Date = DateTime.Today.AddDays(-2), Type = "Môi trường", Description = "Kiểm tra nhiệt độ, độ ẩm và thông gió.", StaffName = "Nhân viên chăm sóc" }
        );

        db.CareSchedules.AddRange(
            new CareSchedule { HerdId = herds[0].Id, Title = "Vệ sinh máng uống định kỳ", ScheduledDate = DateTime.Today.AddDays(1), Status = "Sắp đến hạn", Assignee = "Nhân viên chăm sóc" },
            new CareSchedule { HerdId = herds[1].Id, Title = "Kiểm tra hệ thống thông gió", ScheduledDate = DateTime.Today.AddDays(3), Status = "Sắp đến hạn", Assignee = "Kỹ thuật viên trang trại" },
            new CareSchedule { HerdId = herds[2].Id, Title = "Vệ sinh khu gà đẻ", ScheduledDate = DateTime.Today.AddDays(-1), Status = "Quá hạn", Assignee = "Nhân viên chăm sóc", Notes = "Chưa xác nhận hoàn thành" }
        );

        db.Vaccines.AddRange(
            new VaccineRecord { HerdId = herds[2].Id, VaccineName = "Newcastle", ScheduledDate = DateTime.Today.AddDays(-16), Status = "Chưa tiêm", Notes = "Chưa xác nhận thực hiện" },
            new VaccineRecord { HerdId = herds[0].Id, VaccineName = "Lở mồm long móng", ScheduledDate = DateTime.Today.AddDays(2), Status = "Sắp đến hạn" },
            new VaccineRecord { HerdId = herds[1].Id, VaccineName = "Dịch tả heo", ScheduledDate = DateTime.Today.AddDays(5), Status = "Sắp đến hạn" }
        );

        db.GrowthRecords.AddRange(
            new GrowthRecord { HerdId = herds[0].Id, RecordedDate = DateTime.Today.AddDays(-14), AverageWeight = 212.4m, SampleSize = 25 },
            new GrowthRecord { HerdId = herds[0].Id, RecordedDate = DateTime.Today.AddDays(-7), AverageWeight = 219.8m, SampleSize = 25 },
            new GrowthRecord { HerdId = herds[0].Id, RecordedDate = DateTime.Today, AverageWeight = 227.3m, SampleSize = 25 },
            new GrowthRecord { HerdId = herds[1].Id, RecordedDate = DateTime.Today.AddDays(-7), AverageWeight = 72.5m, SampleSize = 30 },
            new GrowthRecord { HerdId = herds[1].Id, RecordedDate = DateTime.Today, AverageWeight = 78.2m, SampleSize = 30 }
        );

        var feed1 = new FeedItem { Name = "Cám hỗn hợp bò sữa", Unit = "kg", Quantity = 1850, MinimumStock = 700, UnitCost = 11800 };
        var feed2 = new FeedItem { Name = "Cám lợn thịt", Unit = "kg", Quantity = 620, MinimumStock = 650, UnitCost = 12700 };
        var feed3 = new FeedItem { Name = "Cám gà đẻ", Unit = "kg", Quantity = 950, MinimumStock = 500, UnitCost = 11200 };
        db.FeedItems.AddRange(feed1, feed2, feed3);
        await db.SaveChangesAsync();

        db.FeedTransactions.AddRange(
            new FeedTransaction { FeedItemId = feed1.Id, HerdId = herds[0].Id, Date = DateTime.Today.AddDays(-1), TransactionType = "Xuất", Quantity = 180, Notes = "Khẩu phần trong ngày" },
            new FeedTransaction { FeedItemId = feed2.Id, HerdId = herds[1].Id, Date = DateTime.Today.AddDays(-1), TransactionType = "Xuất", Quantity = 150, Notes = "Khẩu phần trong ngày" }
        );

        db.MedicineItems.AddRange(
            new MedicineItem { Name = "Vitamin tổng hợp", Unit = "lọ", Quantity = 22, MinimumStock = 8, ExpiryDate = DateTime.Today.AddMonths(9), Notes = "Vật tư bổ sung dinh dưỡng" },
            new MedicineItem { Name = "Dung dịch sát trùng", Unit = "chai", Quantity = 12, MinimumStock = 10, ExpiryDate = DateTime.Today.AddMonths(5), Notes = "Dùng vệ sinh chuồng trại" },
            new MedicineItem { Name = "Kim tiêm thú y", Unit = "hộp", Quantity = 4, MinimumStock = 5, ExpiryDate = null, Notes = "Vật tư tiêu hao" }
        );

        db.ReproductionRecords.AddRange(
            new ReproductionRecord { HerdId = herds[0].Id, EventType = "Phối giống", Date = DateTime.Today.AddDays(-35), Result = "Đã ghi nhận", NextExpectedDate = DateTime.Today.AddDays(245), Notes = "Theo dõi định kỳ" },
            new ReproductionRecord { HerdId = herds[0].Id, EventType = "Kiểm tra thai", Date = DateTime.Today.AddDays(-5), Result = "Theo dõi tiếp", NextExpectedDate = DateTime.Today.AddDays(25) }
        );

        // Tổng doanh thu mẫu xấp xỉ 211,9 triệu để giữ tinh thần dashboard mẫu.
        db.Sales.AddRange(
            new SaleRecord { HerdId = herds[1].Id, Date = DateTime.Today.AddDays(-25), Quantity = 30, AverageWeight = 95m, UnitPrice = 45000m, Buyer = "HTX Minh Phát" },
            new SaleRecord { HerdId = herds[2].Id, Date = DateTime.Today.AddDays(-12), Quantity = 170, AverageWeight = 2.05m, UnitPrice = 24000m, Buyer = "Cửa hàng Nông Sản Xanh" },
            new SaleRecord { HerdId = herds[0].Id, Date = DateTime.Today.AddDays(-5), Quantity = 4, AverageWeight = 445m, UnitPrice = 42300m, Buyer = "Trang trại An Bình" }
        );

        db.Expenses.AddRange(
            new ExpenseRecord { Date = DateTime.Today.AddDays(-20), Category = "Thức ăn", Description = "Nhập thức ăn tháng", Amount = 68200000m },
            new ExpenseRecord { Date = DateTime.Today.AddDays(-10), Category = "Vaccine", Description = "Vaccine và vật tư thú y", Amount = 12400000m },
            new ExpenseRecord { Date = DateTime.Today.AddDays(-6), Category = "Điện nước", Description = "Chi phí vận hành chuồng", Amount = 8700000m }
        );

        await db.SaveChangesAsync();
    }
}
