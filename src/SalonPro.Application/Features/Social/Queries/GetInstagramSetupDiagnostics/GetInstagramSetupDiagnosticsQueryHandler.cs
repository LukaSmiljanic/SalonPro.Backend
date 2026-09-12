using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetInstagramSetupDiagnostics;

public record InstagramSetupDiagnosticsDto(
    bool MetaConfigured,
    string? AppId,
    string RedirectUri,
    IReadOnlyList<string> OAuthScopes,
    bool CanPublish,
    IReadOnlyList<string> SetupSteps);

public record GetInstagramSetupDiagnosticsQuery : IRequest<InstagramSetupDiagnosticsDto>;

public class GetInstagramSetupDiagnosticsQueryHandler
    : IRequestHandler<GetInstagramSetupDiagnosticsQuery, InstagramSetupDiagnosticsDto>
{
    private readonly IInstagramService _instagramService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetInstagramSetupDiagnosticsQueryHandler(
        IInstagramService instagramService,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _instagramService = instagramService;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<InstagramSetupDiagnosticsDto> Handle(
        GetInstagramSetupDiagnosticsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var scopes = _instagramService.GetRequestedScopes();
        var canPublish = scopes.Any(s =>
            s.Equals("instagram_content_publish", StringComparison.OrdinalIgnoreCase));

        var steps = new List<string>
        {
            "1. Instagram nalog mora biti Business ili Creator i povezan sa Facebook Page.",
            "2. Meta app → App Review → Permissions: dodaj instagram_basic, instagram_content_publish, pages_show_list, pages_read_engagement.",
            "3. Meta app → Use cases → Customize → uključi iste dozvole.",
            "4. Meta app → Facebook Login → Valid OAuth Redirect URI: https://salonpro.netlify.app/instagram/oauth-callback",
            "5. App settings → Basic → App Domains: salonpro.runasp.net i salonpro.netlify.app",
            "6. App roles → ti moraš biti Administrator dok je app Unpublished.",
        };

        if (!canPublish)
        {
            steps.Add("NAPOMENA: Trenutno se traže samo pages_* scopes (test mod). Za objavu postova dodaj instagram_* u Meta OAuthScopes na serveru.");
        }

        return new InstagramSetupDiagnosticsDto(
            _instagramService.IsConfigured,
            _instagramService.AppId,
            _instagramService.RedirectUri ?? string.Empty,
            scopes,
            canPublish,
            steps);
    }
}
