using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetInstagramConnectUrl;

public record GetInstagramConnectUrlQuery : IRequest<InstagramConnectUrl>;

public class GetInstagramConnectUrlQueryHandler : IRequestHandler<GetInstagramConnectUrlQuery, InstagramConnectUrl>
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInstagramService _instagramService;
    private readonly IOAuthStateService _oauthStateService;

    public GetInstagramConnectUrlQueryHandler(
        ICurrentTenantService currentTenantService,
        IUnitOfWork unitOfWork,
        IInstagramService instagramService,
        IOAuthStateService oauthStateService)
    {
        _currentTenantService = currentTenantService;
        _unitOfWork = unitOfWork;
        _instagramService = instagramService;
        _oauthStateService = oauthStateService;
    }

    public async Task<InstagramConnectUrl> Handle(GetInstagramConnectUrlQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        if (!_instagramService.IsConfigured)
            throw new Common.Exceptions.ValidationException("Meta/Instagram aplikacija nije konfigurisana na serveru.");

        var state = _oauthStateService.CreateState(tenantId);
        return new InstagramConnectUrl(_instagramService.BuildConnectUrl(state));
    }
}
