namespace SalonPro.Infrastructure.Meta;

public class MetaSettings
{
    public const string SectionName = "Meta";
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    /// <summary>Must match Meta app settings, e.g. https://your-app.netlify.app/instagram/oauth-callback</summary>
    public string RedirectUri { get; set; } = string.Empty;
    public string GraphApiVersion { get; set; } = "v21.0";
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// OAuth scopes. Override on Monster if Meta rejects instagram_* (try pages_show_list,pages_read_engagement first).
    /// </summary>
    public string[]? OAuthScopes { get; set; }
}
