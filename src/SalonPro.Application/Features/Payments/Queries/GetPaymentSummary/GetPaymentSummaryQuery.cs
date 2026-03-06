using MediatR;
using SalonPro.Application.Features.Payments.DTOs;

namespace SalonPro.Application.Features.Payments.Queries.GetPaymentSummary;

public record GetPaymentSummaryQuery() : IRequest<List<PaymentSummaryDto>>;
