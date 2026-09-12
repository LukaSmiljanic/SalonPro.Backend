using MediatR;
using SalonPro.Application.Features.Social;
using SalonPro.Application.Features.Social.DTOs;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social.Commands.RegeneratePostImage;

public record RegeneratePostImageCommand(Guid PostId) : IRequest<RegenerateImageQueuedDto>;

public class RegeneratePostImageCommandHandler : IRequestHandler<RegeneratePostImageCommand, RegenerateImageQueuedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IOpenAiService _openAiService;
    private readonly ISocialImageQueue _imageQueue;

    public RegeneratePostImageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IOpenAiService openAiService,
        ISocialImageQueue imageQueue)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _openAiService = openAiService;
        _imageQueue = imageQueue;
    }

    public async Task<RegenerateImageQueuedDto> Handle(RegeneratePostImageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        SocialPlanGuard.EnsureAllowed(tenant);

        if (!_openAiService.ImageGenerationEnabled)
            throw new Common.Exceptions.ValidationException("OpenAI generisanje slika nije konfigurisano.");

        var post = await _unitOfWork.SocialPosts.GetByIdAsync(request.PostId, cancellationToken)
            ?? throw new Common.Exceptions.NotFoundException("Objava nije pronađena.");

        if (post.TenantId != tenantId)
            throw new Common.Exceptions.NotFoundException("Objava nije pronađena.");

        if (post.Status == SocialPostStatus.Published)
            throw new Common.Exceptions.ValidationException("Objavljene postove nije moguće menjati.");

        var prompt = post.ImagePrompt
            ?? $"Professional Instagram photo for beauty salon, topic: {post.Topic}, no text on image";

        post.FailureReason = null;
        _unitOfWork.SocialPosts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _imageQueue.EnqueueAsync(new SocialImageJob(tenantId, post.Id, prompt), cancellationToken);

        return new RegenerateImageQueuedDto(
            post.Id,
            "queued",
            "Slika se generiše u pozadini (obično 30–90 sek). Osvežite stranicu.");
    }
}
