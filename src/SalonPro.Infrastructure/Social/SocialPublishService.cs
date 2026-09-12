using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Infrastructure.Persistence;
using SalonPro.Infrastructure.Storage;

namespace SalonPro.Infrastructure.Social;

public class SocialPublishService : ISocialPublishService
{
    private readonly ApplicationDbContext _context;
    private readonly IInstagramService _instagramService;
    private readonly IMediaStorageService _mediaStorage;
    private readonly SocialStorageSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SocialPublishService> _logger;

    public SocialPublishService(
        ApplicationDbContext context,
        IInstagramService instagramService,
        IMediaStorageService mediaStorage,
        IOptions<SocialStorageSettings> settings,
        IConfiguration configuration,
        ILogger<SocialPublishService> logger)
    {
        _context = context;
        _instagramService = instagramService;
        _mediaStorage = mediaStorage;
        _settings = settings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishDuePostAsync(SocialPost post, CancellationToken cancellationToken = default)
    {
        var connection = await _instagramService.GetConnectionStatusAsync(post.TenantId, cancellationToken);
        var now = DateTime.UtcNow;

        if (connection.IsConnected && _instagramService.IsConfigured)
        {
            if (string.IsNullOrWhiteSpace(post.ImageUrl))
            {
                post.Status = SocialPostStatus.Failed;
                post.FailureReason = "Objava nema sliku — potrebna je slika za Instagram.";
                return;
            }

            try
            {
                var caption = string.IsNullOrWhiteSpace(post.Hashtags)
                    ? post.Caption
                    : $"{post.Caption}\n\n{post.Hashtags}";

                var imageUrl = ToAbsoluteImageUrl(post);
                _logger.LogInformation("Publishing post {PostId} with image URL {ImageUrl}", post.Id, imageUrl);

                var mediaId = await _instagramService.PublishImagePostAsync(
                    post.TenantId, imageUrl, caption, cancellationToken);

                post.Status = SocialPostStatus.Published;
                post.PublishedAt = now;
                post.InstagramMediaId = mediaId;
                post.FailureReason = null;
                _logger.LogInformation("Published post {PostId} to Instagram as {MediaId}", post.Id, mediaId);
            }
            catch (Exception ex)
            {
                post.Status = SocialPostStatus.Failed;
                post.FailureReason = ex.Message.Length > 480 ? ex.Message[..480] : ex.Message;
                _logger.LogError(ex, "Failed to publish post {PostId}", post.Id);
            }

            return;
        }

        if (_settings.SimulateInstagramPublish)
        {
            post.Status = SocialPostStatus.Published;
            post.PublishedAt = now;
            post.FailureReason = null;
            _logger.LogInformation("Simulated publish for post {PostId}", post.Id);
            return;
        }

        post.Status = SocialPostStatus.Failed;
        post.FailureReason = "Instagram nalog nije povezan.";
    }

    private string ToAbsoluteImageUrl(SocialPost post)
    {
        var stored = post.ImageUrl ?? string.Empty;
        if (stored.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || stored.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureInstagramFetchableUrl(stored);
        }

        const string prefix = "/media/social/";
        var relative = stored.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? stored[prefix.Length..]
            : stored.TrimStart('/');

        var publicBase = ResolvePublicMediaBaseUrl();
        return $"{publicBase}/media/social/{relative.Replace('\\', '/')}";
    }

    private string EnsureInstagramFetchableUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        const string mediaPath = "/media/social/";
        if (!url.Contains(mediaPath, StringComparison.OrdinalIgnoreCase))
            return url;

        var publicBase = ResolvePublicMediaBaseUrl();
        if (string.IsNullOrWhiteSpace(publicBase))
            return SwapApiHostToCdn(url);

        var idx = url.IndexOf(mediaPath, StringComparison.OrdinalIgnoreCase);
        return idx >= 0
            ? $"{publicBase}{url[idx..].Replace('\\', '/')}"
            : url;
    }

    private static string SwapApiHostToCdn(string url) =>
        url.Replace("salonpro.runasp.net", "salonpro.netlify.app", StringComparison.OrdinalIgnoreCase);

    private string? ResolvePublicMediaBaseUrl()
    {
        var configured = _settings.PublicMediaBaseUrl?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var frontend = _configuration["AppSettings:FrontendUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(frontend))
            return frontend;

        // Monster /media/social is not reliably fetchable by Meta; Netlify proxies it.
        return "https://salonpro.netlify.app";
    }
}
