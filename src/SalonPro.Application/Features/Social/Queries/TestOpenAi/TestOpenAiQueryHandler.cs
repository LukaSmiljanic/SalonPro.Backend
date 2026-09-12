using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.TestOpenAi;

public record OpenAiTestResultDto(
    bool Success,
    string Message,
    bool HasApiKeyInConfig,
    int ApiKeyLength,
    string KeySource);

public record TestOpenAiQuery : IRequest<OpenAiTestResultDto>;

public class TestOpenAiQueryHandler : IRequestHandler<TestOpenAiQuery, OpenAiTestResultDto>
{
    private readonly IOpenAiService _openAiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public TestOpenAiQueryHandler(
        IOpenAiService openAiService,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _openAiService = openAiService;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<OpenAiTestResultDto> Handle(TestOpenAiQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        var meta = (
            _openAiService.HasApiKey,
            _openAiService.ApiKeyLength,
            _openAiService.KeySource);

        if (!_openAiService.IsConfigured)
        {
            return new OpenAiTestResultDto(
                false,
                _openAiService.LastError ?? "OpenAI nije konfigurisan (ključ ili Enabled).",
                meta.Item1,
                meta.Item2,
                meta.Item3);
        }

        var json = await _openAiService.GenerateChatJsonAsync(
            "You are a connectivity test. Reply only with JSON.",
            "{\"ping\":true}",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new OpenAiTestResultDto(
                false,
                _openAiService.LastError ?? "OpenAI nije odgovorio.",
                meta.Item1,
                meta.Item2,
                meta.Item3);
        }

        return new OpenAiTestResultDto(
            true,
            "OpenAI radi — test poruka uspešna.",
            meta.Item1,
            meta.Item2,
            meta.Item3);
    }
}
