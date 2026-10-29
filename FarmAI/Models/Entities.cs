using System.ComponentModel.DataAnnotations;

namespace FarmAI.Models;

// All persisted identifiers are server-generated UUIDs. Monetary amounts use decimal.
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(64)] public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(16)] public string Role { get; set; } = Roles.Employee;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public static class Roles
{
    public const string Manager = "MANAGER";
    public const string Technician = "TECHNICIAN";
    public const string Employee = "EMPLOYEE";
}
public class LoginSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
public class Assignment
{
    public Guid UserId { get; set; }
    public Guid BarnId { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
}
public class Barn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(32)] public string Code { get; set; } = "";
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Species { get; set; } = "";
    public int Capacity { get; set; }
    [MaxLength(16)] public string Status { get; set; } = "ACTIVE";
}
public class Herd
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(32)] public string Code { get; set; } = "";
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Species { get; set; } = "";
    [MaxLength(80)] public string? Breed { get; set; }
    [MaxLength(80)] public string? Purpose { get; set; }
    public Guid BarnId { get; set; }
    public Barn? Barn { get; set; }
    public DateOnly ReceivedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int Quantity { get; set; }
    [MaxLength(16)] public string Status { get; set; } = "ACTIVE";
    public int Version { get; set; } = 1;
}
public class Allocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public Guid BarnId { get; set; }
    public DateTime FromAt { get; set; } = DateTime.UtcNow;
    public DateTime? UntilAt { get; set; }
}
public class HerdMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public int Delta { get; set; }
    [MaxLength(24)] public string Kind { get; set; } = "";
    public Guid SourceId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public bool Approved { get; set; }
    [MaxLength(300)] public string Reason { get; set; } = "";
}
public class CareLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    [MaxLength(16)] public string Kind { get; set; } = "FEED";
    [MaxLength(100)] public string ItemName { get; set; } = "";
    [MaxLength(20)] public string Unit { get; set; } = "kg";
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Cost => UnitPrice.HasValue ? Math.Round(UnitPrice.Value * Quantity, 0, MidpointRounding.AwayFromZero) : null;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    [MaxLength(16)] public string Status { get; set; } = "PENDING";
    [MaxLength(1000)] public string? Reference { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
}
public class GrowthRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public int SampleSize { get; set; }
    public decimal WeightKg { get; set; }
    [MaxLength(32)] public string Method { get; set; } = "RANDOM";
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public decimal AverageKg => SampleSize > 0 ? Math.Round(WeightKg / SampleSize, 3) : 0;
}
public class Animal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    [MaxLength(48)] public string Tag { get; set; } = "";
    [MaxLength(12)] public string Sex { get; set; } = "FEMALE";
    public DateOnly? BirthDate { get; set; }
}
public class ReproductionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MotherId { get; set; }
    public Guid TargetHerdId { get; set; }
    public int LiveBorn { get; set; }
    public int StillBorn { get; set; }
    public int Accepted { get; set; }
    public DateOnly EventDate { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid IdempotencyKey { get; set; }
}
public class Vaccine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Unit { get; set; } = "dose";
    public bool Active { get; set; } = true;
}
public class VaccinationPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public Guid VaccineId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public int Target { get; set; }
    public Guid AssignedTo { get; set; }
    [MaxLength(16)] public string Status { get; set; } = "PLANNED";
    [MaxLength(1000)] public string Reference { get; set; } = "";
    public int Version { get; set; } = 1;
}
public class VaccinationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public int Quantity { get; set; }
    [MaxLength(80)] public string Lot { get; set; } = "";
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public Guid PerformedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid IdempotencyKey { get; set; }
}
public class FarmNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid RecipientId { get; set; }
    [MaxLength(16)] public string Kind { get; set; } = "DUE";
    public DateOnly RemindDate { get; set; }
    [MaxLength(320)] public string Message { get; set; } = "";
}
public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(160)] public string CustomerName { get; set; } = "";
    [MaxLength(16)] public string Status { get; set; } = "DRAFT";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SoldAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public Guid? ConfirmKey { get; set; }
    public string? PayloadHash { get; set; }
    public List<SaleLine> Lines { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
public class SaleLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public Guid HerdId { get; set; }
    public int Quantity { get; set; }
    public decimal? WeightKg { get; set; }
    [MaxLength(8)] public string Basis { get; set; } = "HEAD";
    public decimal UnitPrice { get; set; }
    public decimal Amount => FarmRules.LineTotal(Basis, Quantity, WeightKg, UnitPrice);
}
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Method { get; set; } = "CASH";
    [MaxLength(100)] public string? Reference { get; set; }
}
public class FarmSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HerdId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string DataJson { get; set; } = "{}";
    [MaxLength(64)] public string Checksum { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AiReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnapshotId { get; set; }
    public string ContentJson { get; set; } = "{}";
    [MaxLength(32)] public string Provider { get; set; } = "RULE_BASED";
    [MaxLength(100)] public string Model { get; set; } = "";
    [MaxLength(32)] public string PromptVersion { get; set; } = "v3";
    [MaxLength(16)] public string Status { get; set; } = "DRAFT";
    public Guid? ReviewerId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(80)] public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string ChangesJson { get; set; } = "{}";
    [MaxLength(80)] public string RequestId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
