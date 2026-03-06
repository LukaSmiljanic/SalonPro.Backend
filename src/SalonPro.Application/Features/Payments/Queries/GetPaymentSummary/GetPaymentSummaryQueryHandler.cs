using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Payments.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Payments.Queries.GetPaymentSummary;

public class GetPaymentSummaryQueryHandler : IRequestHandler<GetPaymentSummaryQuery, List<PaymentSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPaymentSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PaymentSummaryDto>> Handle(GetPaymentSummaryQuery request, CancellationToken cancellationToken)
    {
        var payments = await _unitOfWork.Payments.Query()
            .Include(p => p.Tenant)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var summaries = payments
            .GroupBy(p => p.TenantId)
            .Select(g => new PaymentSummaryDto(
                g.Key,
                g.First().Tenant.Name,
                g.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
                g.Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Overdue).Sum(p => p.Amount),
                g.Where(p => p.Status == PaymentStatus.Paid)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => p.PaidAt)
                    .FirstOrDefault()
            ))
            .ToList();

        return summaries;
    }
}
