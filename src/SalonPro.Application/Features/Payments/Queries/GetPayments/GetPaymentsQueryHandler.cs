using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Payments.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Payments.Queries.GetPayments;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, List<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPaymentsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Payments.Query()
            .Include(p => p.Tenant)
            .AsNoTracking();

        if (request.TenantId.HasValue)
        {
            query = query.Where(p => p.TenantId == request.TenantId.Value);
        }

        if (request.Year.HasValue)
        {
            query = query.Where(p => p.PeriodStart.Year == request.Year.Value);
        }

        if (request.Month.HasValue)
        {
            query = query.Where(p => p.PeriodStart.Month == request.Month.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PeriodStart)
            .ToListAsync(cancellationToken);

        return payments.Select(p => new PaymentDto(
            p.Id,
            p.TenantId,
            p.Tenant.Name,
            p.Amount,
            p.Currency,
            p.PeriodStart,
            p.PeriodEnd,
            p.Status,
            p.PaidAt,
            p.Notes,
            p.PaidBy,
            p.CreatedAt
        )).ToList();
    }
}
