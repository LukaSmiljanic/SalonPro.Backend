using MediatR;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Social;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Queries.TestOpenAiImage;

public record OpenAiImageTestResultDto(bool Success, string Message);

public record TestOpenAiImageQuery : IRequest<OpenAiImageTestResultDto>;

public class TestOpenAiImageQueryHandler : IRequestHandler<TestOpenAiImageQuery, OpenAiImageTestResultDto>
{
    private readonly IOpenAiService _openAiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public TestOpenAiImageQueryHandler(
        IOpenAiService openAiService,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService)
    {
        _openAiService = openAiService;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<OpenAiImageTestResultDto> Handle(TestOpenAiImageQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        if (!_openAiService.ImageGenerationEnabled)
        {
            return new OpenAiImageTestResultDto(
                false,
                _openAiService.LastError ?? "OpenAI generisanje slika nije uključeno.");
        }

        var bytes = await _openAiService.GenerateImageAsync(
            "Simple solid pastel pink square for a beauty salon Instagram post test, no text",
            cancellationToken);

        if (bytes == null || bytes.Length == 0)
        {
            return new OpenAiImageTestResultDto(
                false,
                _openAiService.LastError ?? "OpenAI nije vratio sliku.");
        }

        return new OpenAiImageTestResultDto(
            true,
            $"OpenAI slike rade — test PNG ({bytes.Length / 1024} KB).");
    }
}
