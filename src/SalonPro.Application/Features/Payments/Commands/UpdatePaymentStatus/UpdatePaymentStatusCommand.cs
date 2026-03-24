using MediatR;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Payments.Commands.UpdatePaymentStatus;

public record UpdatePaymentStatusCommand(
    Guid Id,
    PaymentStatus Status,
    string? Notes = null,
    string? PaidBy = null
) : IRequest<Unit>;
