using SalonPro.Domain.Common;
using SalonPro.Domain.Enums;

namespace SalonPro.Domain.Entities;

public class Payment : BaseAuditableEntity
{
    public Guid TenantId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public string? PaidBy { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
