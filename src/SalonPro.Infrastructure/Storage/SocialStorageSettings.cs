namespace SalonPro.Infrastructure.Storage;

public class SocialStorageSettings
{
    public const string SectionName = "Social";
    public string StoragePath { get; set; } = "social-media";
    /// <summary>Public HTTPS base URL of API, e.g. https://salonpro.runasp.net</summary>
    public string ApiPublicUrl { get; set; } = string.Empty;
    /// <summary>
    /// Base URL Meta/Instagram uses to fetch images (must return image/png publicly).
    /// Use Netlify proxy in production: https://salonpro.netlify.app
    /// </summary>
    public string PublicMediaBaseUrl { get; set; } = string.Empty;
    public bool PublishJobEnabled { get; set; } = true;
  /// <summary>When true and IG not connected, mark posts as published (dev).</summary>
    public bool SimulateInstagramPublish { get; set; } = true;
}
