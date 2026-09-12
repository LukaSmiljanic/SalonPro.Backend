using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Infrastructure.Social;

/// <summary>
/// Template-based post generator using salon data. Replace with LLM (OpenAI) later.
/// </summary>
public class SalonSocialContentGenerator : ISocialContentGenerator
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] DefaultPostHours = ["10:00", "12:30", "17:00", "11:00", "16:00", "10:30", "18:00"];

    public SalonSocialContentGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SocialPostDraft>> GenerateWeekAsync(
        Guid tenantId,
        DateTime weekStartLocalDate,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        var services = await _unitOfWork.Services.Query()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .Take(8)
            .ToListAsync(cancellationToken);

        var city = string.IsNullOrWhiteSpace(tenant.City) ? "vašem gradu" : tenant.City.Trim();
        var salon = tenant.Name.Trim();
        var serviceList = services.Count > 0 ? string.Join(", ", services.Take(4)) : "šišanje, feniranje, tretmani";
        var tz = ResolveTimeZone(tenant.TimeZone);

        var topics = new[]
        {
            ("Pon — dobrodošlica", $"✨ Novi tjedan, nova energija u {salon}!\n\nSpremni smo za transformacije — od {serviceList}.\n\n📍 {city}\n📩 Pišite nam u DM za termin."),
            ("Utorak — savet", $"💡 Savet dana iz {salon}:\n\nRedovno negovanje kose čuva sjaj između tretmana. Zakazite refresh feniranja ili masku — koža glave će vam biti zahvalna!\n\nKoje usluge vas zanimaju? {serviceList}"),
            ("Sreda — ponuda", $"🌸 Sredinom nedelje zaslužujete pauzu za sebe.\n\nU {salon} vas čekaju: {serviceList}.\n\nRezervišite termin — broj mesta je ograničen."),
            ("Četvrtak — transformacija", $"✂️ Before & after energija u {salon}!\n\nSvaka promena kose je mali ritual samopouzdanja. Podelite sa nama šta želite da postignemo — mi ćemo ostatak.\n\n📍 {city}"),
            ("Petak — vikend", $"🔥 Petak je dan za glam u {salon}!\n\nSpremni za vikend? Feniranje, šminka, nokti — sve na jednom mestu.\n\nUsluge: {serviceList}\n\nDM za brzu rezervaciju."),
            ("Subota — klijenti", $"💜 Hvala što ste deo {salon} porodice!\n\nVaše zadovoljstvo nam je najlepša preporuka. Tagujte nas u objavama — volimo da vidimo vaše rezultate!\n\n#{salon.Replace(" ", "")} #{city.Replace(" ", "")}"),
            ("Nedelja — inspiracija", $"☀️ Nedelja je za planiranje i self-care.\n\nSledeća nedelja u {salon} — rezervišite termin unapred i obezbedite svoje mesto.\n\n{serviceList} — tu smo za vas."),
        };

        var baseTags = $"#{Slug(salon)} #salon #{Slug(city)} #beauty #frizerskisalon #nokti #selfcare";
        var drafts = new List<SocialPostDraft>();

        for (var i = 0; i < 7; i++)
        {
            var day = weekStartLocalDate.Date.AddDays(i);
            var (topic, caption) = topics[i];
            var localTime = TimeSpan.Parse(DefaultPostHours[i]);
            var localDt = day.Add(localTime);
            var utc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDt, DateTimeKind.Unspecified),
                tz);

            var dayTags = i switch
            {
                4 => $"{baseTags} #petak #vikend",
                5 => $"{baseTags} #subota #klijenti",
                0 => $"{baseTags} #ponedeljak #novinedelja",
                _ => baseTags,
            };

            var imagePrompt =
                $"Instagram post for beauty salon \"{salon}\" in {city}, topic: {topic}, " +
                $"elegant purple and white aesthetic, professional salon interior or hair styling, no text overlay";

            drafts.Add(new SocialPostDraft(topic, caption, dayTags, imagePrompt, utc));
        }

        return drafts;
    }

    public async Task<SocialPostDraft> GenerateFromGalleryAsync(
        Guid tenantId,
        string? captionHint,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        var city = string.IsNullOrWhiteSpace(tenant.City) ? "vašem gradu" : tenant.City.Trim();
        var salon = tenant.Name.Trim();
        var hint = string.IsNullOrWhiteSpace(captionHint) ? "fotografija iz salona" : captionHint.Trim();
        var tz = ResolveTimeZone(tenant.TimeZone);
        var tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var utc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(tomorrow, DateTimeKind.Unspecified), tz);

        var caption = $"📸 {salon}\n\n{hint}\n\n📍 {city}\n📩 Pišite nam za termin.";
        var tags = $"#{Slug(salon)} #salon #{Slug(city)} #beauty #frizerskisalon #galerija";
        return new SocialPostDraft("Galerija", caption, tags, string.Empty, utc);
    }

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch { /* fallback */ }
        }

        try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
        catch { return TimeZoneInfo.Utc; }
    }

    private static string Slug(string value) =>
        new string(value.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .ToArray())
            .Replace(' ', '_');
}
