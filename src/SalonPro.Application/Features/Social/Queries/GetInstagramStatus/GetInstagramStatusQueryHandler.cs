using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetInstagramStatus;

public record GetInstagramStatusQuery : IRequest<InstagramConnectionStatus>;

public class GetInstagramStatusQueryHandler : IRequestHandler<GetInstagramStatusQuery, InstagramConnectionStatus>
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInstagramService _instagramService;

    public GetInstagramStatusQueryHandler(
        ICurrentTenantService currentTenantService,
        IUnitOfWork unitOfWork,
        IInstagramService instagramService)
    {
        _currentTenantService = currentTenantService;
        _unitOfWork = unitOfWork;
        _instagramService = instagramService;
    }

    public async Task<InstagramConnectionStatus> Handle(GetInstagramStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        return await _instagramService.GetConnectionStatusAsync(tenantId, cancellationToken);
    }
}
