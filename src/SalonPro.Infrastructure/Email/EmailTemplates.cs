namespace SalonPro.Infrastructure.Email;

/// <summary>
/// Inline HTML email templates for appointment notifications.
/// Clean, modern design with deep plum/purple palette matching SalonPro brand.
/// </summary>
public static class EmailTemplates
{
    private const string PrimaryColor = "#5B3A8C";
    private const string LightBg = "#F8F6FB";
    private const string DarkText = "#2D2040";
    private const string MutedText = "#6B6080";
    private const string SuccessColor = "#2E7D32";
    private const string DangerColor = "#C62828";
    private const string WarningColor = "#E65100";

    private static string BaseLayout(string title, string accentColor, string iconEmoji, string body, string salonName)
    {
        return $@"<!DOCTYPE html>
<html lang=""sr"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background-color:{LightBg};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{LightBg};padding:32px 16px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;width:100%;"">
          <!-- Header -->
          <tr>
            <td style=""background-color:{accentColor};border-radius:12px 12px 0 0;padding:28px 32px;text-align:center;"">
              <div style=""font-size:36px;margin-bottom:8px;"">{iconEmoji}</div>
              <h1 style=""margin:0;color:#ffffff;font-size:22px;font-weight:600;"">{title}</h1>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""background-color:#ffffff;padding:32px;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.06);"">
              {body}
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""padding:24px 32px;text-align:center;"">
              <p style=""margin:0;color:{MutedText};font-size:13px;"">{salonName} &bull; Powered by SalonPro</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string DetailRow(string label, string value)
    {
        return $@"
            <tr>
              <td style=""padding:10px 0;color:{MutedText};font-size:14px;width:140px;vertical-align:top;"">{label}</td>
              <td style=""padding:10px 0;color:{DarkText};font-size:14px;font-weight:500;"">{value}</td>
            </tr>";
    }

    private static string DetailsTable(string dateFormatted, string timeFormatted, string services, string staff, string duration, string price)
    {
        return $@"
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
            {DetailRow("📅 Datum", dateFormatted)}
            {DetailRow("🕐 Vreme", timeFormatted)}
            {DetailRow("💇 Usluge", services)}
            {DetailRow("👤 Zaposleni", staff)}
            {DetailRow("⏱ Trajanje", duration + " min")}
            {DetailRow("💰 Cena", price)}
          </table>";
    }

    public static string AppointmentConfirmation(
        string clientName, string salonName, string dateFormatted, string timeFormatted,
        string services, string staffName, int durationMinutes, string price,
        string? salonPhone, string? salonAddress)
    {
        var contactInfo = "";
        if (!string.IsNullOrWhiteSpace(salonPhone) || !string.IsNullOrWhiteSpace(salonAddress))
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(salonPhone)) parts.Add($"📞 {salonPhone}");
            if (!string.IsNullOrWhiteSpace(salonAddress)) parts.Add($"📍 {salonAddress}");
            contactInfo = $@"<p style=""margin:16px 0 0;color:{MutedText};font-size:13px;"">{string.Join(" &bull; ", parts)}</p>";
        }

        var body = $@"
            <p style=""margin:0 0 8px;color:{DarkText};font-size:16px;"">Poštovani <strong>{clientName}</strong>,</p>
            <p style=""margin:0 0 20px;color:{MutedText};font-size:14px;line-height:1.6;"">
              Vaš termin je uspešno zakazan. Ispod su detalji:
            </p>
            {DetailsTable(dateFormatted, timeFormatted, services, staffName, durationMinutes.ToString(), price)}
            <div style=""background-color:{LightBg};border-radius:8px;padding:16px;margin:20px 0;"">
              <p style=""margin:0;color:{DarkText};font-size:13px;line-height:1.5;"">
                💡 Ako želite da promenite ili otkažete termin, kontaktirajte nas blagovremeno.
              </p>
            </div>
            {contactInfo}";

        return BaseLayout("Potvrda termina", SuccessColor, "✅", body, salonName);
    }

    public static string AppointmentReminder(
        string clientName, string salonName, string dateFormatted, string timeFormatted,
        string services, string staffName, int durationMinutes, string price,
        string? salonPhone, string? salonAddress)
    {
        var contactInfo = "";
        if (!string.IsNullOrWhiteSpace(salonPhone) || !string.IsNullOrWhiteSpace(salonAddress))
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(salonPhone)) parts.Add($"📞 {salonPhone}");
            if (!string.IsNullOrWhiteSpace(salonAddress)) parts.Add($"📍 {salonAddress}");
            contactInfo = $@"<p style=""margin:16px 0 0;color:{MutedText};font-size:13px;"">{string.Join(" &bull; ", parts)}</p>";
        }

        var body = $@"
            <p style=""margin:0 0 8px;color:{DarkText};font-size:16px;"">Poštovani <strong>{clientName}</strong>,</p>
            <p style=""margin:0 0 20px;color:{MutedText};font-size:14px;line-height:1.6;"">
              Podsetnik — imate zakazan termin sutra. Radujemo se vašem dolasku!
            </p>
            {DetailsTable(dateFormatted, timeFormatted, services, staffName, durationMinutes.ToString(), price)}
            <div style=""background-color:#FFF3E0;border-radius:8px;padding:16px;margin:20px 0;border-left:4px solid {WarningColor};"">
              <p style=""margin:0;color:{DarkText};font-size:13px;line-height:1.5;"">
                ⏰ Vaš termin je sutra u <strong>{timeFormatted}</strong>. Molimo vas da dođete na vreme.
              </p>
            </div>
            {contactInfo}";

        return BaseLayout("Podsetnik za sutrašnji termin", WarningColor, "🔔", body, salonName);
    }

    public static string EmailVerification(string tenantName, string verificationUrl)
    {
        var body = $@"
            <p style=""margin:0 0 8px;color:{DarkText};font-size:16px;"">Dobrodošli u <strong>SalonPro</strong>!</p>
            <p style=""margin:0 0 24px;color:{MutedText};font-size:14px;line-height:1.6;"">
              Vaš salon <strong>{tenantName}</strong> je registrovan. Kliknite na dugme ispod da aktivirate nalog i započnete 30 dana besplatnog korišćenja.
            </p>
            <div style=""text-align:center;margin:28px 0;"">
              <a href=""{verificationUrl}"" 
                 style=""display:inline-block;background-color:{PrimaryColor};color:#ffffff;font-size:16px;font-weight:600;text-decoration:none;padding:14px 36px;border-radius:8px;"">
                Aktiviraj nalog
              </a>
            </div>
            <p style=""margin:0;color:{MutedText};font-size:13px;line-height:1.5;"">
              Ako dugme ne radi, kopirajte ovaj link u pretraživač:
            </p>
            <p style=""margin:8px 0 0;color:{PrimaryColor};font-size:13px;word-break:break-all;"">
              {verificationUrl}
            </p>
            <div style=""background-color:{LightBg};border-radius:8px;padding:16px;margin:24px 0 0;"">
              <p style=""margin:0;color:{MutedText};font-size:13px;line-height:1.5;"">
                ⏰ Link je aktivan 48 sati. Nakon aktivacije dobijate 30 dana besplatnog trial perioda.
              </p>
            </div>";

        return BaseLayout("Aktivacija naloga", PrimaryColor, "🚀", body, "SalonPro");
    }

    public static string AppointmentCancellation(
        string clientName, string salonName, string dateFormatted, string timeFormatted,
        string services, string staffName, string? reason)
    {
        var reasonBlock = "";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            reasonBlock = $@"
            <div style=""background-color:#FFEBEE;border-radius:8px;padding:16px;margin:20px 0;border-left:4px solid {DangerColor};"">
              <p style=""margin:0;color:{DarkText};font-size:13px;line-height:1.5;"">
                <strong>Razlog otkazivanja:</strong> {reason}
              </p>
            </div>";
        }

        var body = $@"
            <p style=""margin:0 0 8px;color:{DarkText};font-size:16px;"">Poštovani <strong>{clientName}</strong>,</p>
            <p style=""margin:0 0 20px;color:{MutedText};font-size:14px;line-height:1.6;"">
              Obaveštavamo Vas da je Vaš termin otkazan.
            </p>
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
              {DetailRow("📅 Datum", dateFormatted)}
              {DetailRow("🕐 Vreme", timeFormatted)}
              {DetailRow("💇 Usluge", services)}
              {DetailRow("👤 Zaposleni", staffName)}
            </table>
            {reasonBlock}
            <p style=""margin:20px 0 0;color:{MutedText};font-size:14px;line-height:1.6;"">
              Za zakazivanje novog termina, kontaktirajte nas ili posetite našu stranicu.
            </p>";

        return BaseLayout("Termin otkazan", DangerColor, "❌", body, salonName);
    }
}
