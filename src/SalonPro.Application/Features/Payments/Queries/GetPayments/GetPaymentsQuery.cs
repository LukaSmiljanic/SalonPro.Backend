using MediatR;
using SalonPro.Application.Features.Payments.DTOs;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Payments.Queries.GetPayments;

public record GetPaymentsQuery(
    Guid? TenantId = null,
    int? Year = null,
    int? Month = null,
    PaymentStatus? Status = null
) : IRequest<List<PaymentDto>>;
