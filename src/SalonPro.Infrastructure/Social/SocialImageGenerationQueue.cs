using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Social;

/// <summary>
/// LongRunning thread per job — survives IIS request end on MonsterASP shared hosting.
/// </summary>
public class SocialImageGenerationQueue : ISocialImageQueue
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocialImageGenerationQueue> _logger;

    public SocialImageGenerationQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<SocialImageGenerationQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ValueTask EnqueueAsync(SocialImageJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Queued social image job for post {PostId} (gallery={GalleryId})",
            job.PostId,
            job.SourceGalleryImageId);

        _ = Task.Factory.StartNew(
            () => RunJobSafeAsync(job),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        return ValueTask.CompletedTask;
    }

    private async Task RunJobSafeAsync(SocialImageJob job)
    {
        await Gate.WaitAsync();
        try
        {
            await ProcessJobAsync(job, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Social image job failed for post {PostId}", job.PostId);
            await TrySetFailureAsync(job, ex.Message);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task ProcessJobAsync(SocialImageJob job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        tenantService.SetTenant(job.TenantId);

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var openAi = scope.ServiceProvider.GetRequiredService<IOpenAiService>();
        var storage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();

        var post = await unitOfWork.SocialPosts.GetByIdAsync(job.PostId, cancellationToken);
        if (post == null || post.TenantId != job.TenantId)
            return;

        _logger.LogInformation("Generating image for post {PostId}", job.PostId);

        byte[]? bytes;
        if (job.SourceGalleryImageId is Guid galleryId)
        {
            bytes = await GenerateGalleryVariantAsync(
                unitOfWork, openAi, storage, job.TenantId, galleryId, job.Prompt, cancellationToken);
        }
        else
        {
            bytes = await openAi.GenerateImageAsync(job.Prompt, cancellationToken);
        }

        if (bytes == null || bytes.Length == 0)
        {
            post.FailureReason = openAi.LastError ?? "Generisanje slike nije uspelo.";
            unitOfWork.SocialPosts.Update(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var relative = await storage.SaveImageAsync(post.TenantId, post.Id, bytes, cancellationToken);
        post.ImageUrl = storage.ToRelativeMediaPath(relative);
        post.ImagePrompt = job.Prompt;
        post.FailureReason = null;

        if (job.SourceGalleryImageId is not null)
        {
            var contentGenerator = scope.ServiceProvider.GetRequiredService<ISocialContentGenerator>();
            var draft = await contentGenerator.GenerateFromGalleryAsync(
                job.TenantId,
                job.CaptionHint,
                bytes,
                "image/png",
                cancellationToken);

            post.Topic = draft.Topic;
            post.Caption = draft.Caption;
            post.Hashtags = draft.Hashtags;
            post.ScheduledAt = draft.ScheduledAtUtc;
        }

        unitOfWork.SocialPosts.Update(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Image saved for post {PostId}: {Url}", job.PostId, post.ImageUrl);
    }

    private static async Task<byte[]?> GenerateGalleryVariantAsync(
        IUnitOfWork unitOfWork,
        IOpenAiService openAi,
        IMediaStorageService storage,
        Guid tenantId,
        Guid galleryId,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var image = await unitOfWork.SocialGalleryImages.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == galleryId && g.TenantId == tenantId, cancellationToken);

        if (image == null)
            throw new InvalidOperationException("Referentna slika iz galerije nije pronađena.");

        var sourceBytes = await storage.ReadByRelativePathAsync(image.RelativePath, cancellationToken);
        if (sourceBytes is not { Length: > 0 })
            throw new InvalidOperationException("Referentna slika nije pronađena na disku.");

        var editPrompt = $"""
            Create a new professional Instagram marketing photo based on this reference image.
            CRITICAL: Preserve all branding, logos, product packaging, labels, colors, and key visual identity exactly as in the original.
            Keep the same product or subject — only apply these creative changes: {userPrompt}
            High quality, natural lighting, no unwanted text overlays.
            """;

        return await openAi.EditImageAsync(sourceBytes, image.ContentType, editPrompt, cancellationToken);
    }

    private async Task TrySetFailureAsync(SocialImageJob job, string message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
            tenantService.SetTenant(job.TenantId);

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var post = await unitOfWork.SocialPosts.GetByIdAsync(job.PostId);
            if (post == null) return;

            post.FailureReason = message.Length > 500 ? message[..500] : message;
            unitOfWork.SocialPosts.Update(post);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not persist failure for post {PostId}", job.PostId);
        }
    }
}
