using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.GetSocialConfig;

public record SocialConfigDto(
    bool OpenAiConfigured,
    bool OpenAiImagesEnabled,
    bool OpenAiEnabled,
    bool HasApiKeyInConfig,
    int ApiKeyLength,
    string KeySource,
    string? LastOpenAiError);

public record GetSocialConfigQuery : IRequest<SocialConfigDto>;

public class GetSocialConfigQueryHandler : IRequestHandler<GetSocialConfigQuery, SocialConfigDto>
{
    private readonly IOpenAiService _openAiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetSocialConfigQueryHandler(
        IOpenAiService openAiService,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _openAiService = openAiService;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<SocialConfigDto> Handle(GetSocialConfigQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        return new SocialConfigDto(
            _openAiService.IsConfigured,
            _openAiService.ImageGenerationEnabled,
            _openAiService.Enabled,
            _openAiService.HasApiKey,
            _openAiService.ApiKeyLength,
            _openAiService.KeySource,
            _openAiService.LastError);
    }
}
