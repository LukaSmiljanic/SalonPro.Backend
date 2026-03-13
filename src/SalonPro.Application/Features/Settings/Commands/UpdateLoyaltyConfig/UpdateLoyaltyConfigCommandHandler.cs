using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Commands.UpdateLoyaltyConfig;

public class UpdateLoyaltyConfigCommandHandler : IRequestHandler<UpdateLoyaltyConfigCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public UpdateLoyaltyConfigCommandHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<Unit> Handle(UpdateLoyaltyConfigCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID je obavezan za ažuriranje loyalty konfiguracije.");

        // Delete existing loyalty config for this tenant
        var existing = await _unitOfWork.LoyaltyConfigs.Query()
            .Where(lc => lc.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
            _unitOfWork.LoyaltyConfigs.Remove(item);

        // Insert new tiers
        foreach (var tier in request.Tiers)
        {
            var config = new LoyaltyConfig
            {
                TenantId = tenantId,
                TierName = tier.TierName,
                MinVisits = tier.MinVisits,
                Benefit = tier.Benefit
            };
            await _unitOfWork.LoyaltyConfigs.AddAsync(config, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
