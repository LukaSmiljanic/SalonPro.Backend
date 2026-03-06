using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Payments.DTOs;

public record PaymentDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    decimal Amount,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    PaymentStatus Status,
    DateTime? PaidAt,
    string? Notes,
    string? PaidBy,
    DateTime CreatedAt
);

public record PaymentSummaryDto(
    Guid TenantId,
    string TenantName,
    decimal TotalPaid,
    decimal TotalPending,
    DateTime? LastPaymentDate
);
