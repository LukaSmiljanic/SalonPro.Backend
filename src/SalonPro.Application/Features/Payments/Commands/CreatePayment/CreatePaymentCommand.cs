using MediatR;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Payments.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid TenantId,
    decimal Amount,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    PaymentStatus Status = PaymentStatus.Pending,
    string? Notes = null,
    string? PaidBy = null
) : IRequest<Guid>;
