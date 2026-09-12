namespace SalonPro.Infrastructure.OpenAi;



public class OpenAiSettings

{

    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string TextModel { get; set; } = "gpt-4o-mini";

    /// <summary>GPT Image model (dall-e-2/3 removed from API May 2026).</summary>

    public string ImageModel { get; set; } = "gpt-image-1-mini";

    /// <summary>low | medium | high — low is fastest/cheapest for social drafts.</summary>

    public string ImageQuality { get; set; } = "low";

    public bool Enabled { get; set; } = true;

    public bool GenerateImages { get; set; } = true;

    /// <summary>Set at startup — not from JSON.</summary>

    public string KeySource { get; set; } = "none";

}


