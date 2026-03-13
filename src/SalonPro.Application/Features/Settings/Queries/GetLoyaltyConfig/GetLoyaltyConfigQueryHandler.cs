using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Queries.GetLoyaltyConfig;

public class GetLoyaltyConfigQueryHandler : IRequestHandler<GetLoyaltyConfigQuery, List<LoyaltyConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetLoyaltyConfigQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<List<LoyaltyConfigDto>> Handle(GetLoyaltyConfigQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za dohvatanje loyalty konfiguracije.");

        var configs = await _unitOfWork.LoyaltyConfigs.Query()
            .Where(lc => lc.TenantId == tenantId)
            .OrderBy(lc => lc.MinVisits)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return configs
            .Select(lc => new LoyaltyConfigDto(lc.TierName, lc.MinVisits, lc.Benefit))
            .ToList();
    }
}
