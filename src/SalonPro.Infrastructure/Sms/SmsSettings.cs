namespace SalonPro.Infrastructure.Sms;

public class SmsSettings
{
    public const string SectionName = "SmsSettings";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.infobip.com";
    public string SenderName { get; set; } = "SalonPro";
    public bool Enabled { get; set; } = false;
}
