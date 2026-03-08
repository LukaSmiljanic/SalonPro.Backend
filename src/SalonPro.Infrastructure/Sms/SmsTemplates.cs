namespace SalonPro.Infrastructure.Sms;

public static class SmsTemplates
{
    /// <summary>
    /// SMS confirmation when appointment is booked.
    /// Example: "Poštovana Jelena, Vaš termin u Demo Salon je potvrđen: 15.03.2026 u 10:00, Šišanje kod Ana (30 min). Cena: 1.500 RSD. Za otkazivanje pozovite 011/1234567."
    /// </summary>
    public static string AppointmentConfirmation(
        string clientName,
        string salonName,
        string date,
        string time,
        string serviceNames,
        string staffName,
        int durationMinutes,
        string price,
        string? salonPhone)
    {
        var greeting = $"Poštovani/a {clientName}";
        var body = $"Vaš termin u {salonName} je potvrđen: {date} u {time}, {serviceNames} kod {staffName} ({durationMinutes} min). Cena: {price}.";

        if (!string.IsNullOrWhiteSpace(salonPhone))
        {
            body += $" Za otkazivanje pozovite {salonPhone}.";
        }

        return $"{greeting}, {body}";
    }

    /// <summary>
    /// SMS reminder 24h before appointment.
    /// Example: "Podsetnik: Sutra 15.03.2026 u 10:00 imate termin u Demo Salon — Šišanje kod Ana. Vidimo se!"
    /// </summary>
    public static string AppointmentReminder(
        string clientName,
        string salonName,
        string date,
        string time,
        string serviceNames,
        string staffName)
    {
        return $"Podsetnik: Sutra {date} u {time} imate termin u {salonName} — {serviceNames} kod {staffName}. Vidimo se!";
    }

    /// <summary>
    /// SMS when appointment is cancelled.
    /// Example: "Obaveštenje: Vaš termin u Demo Salon za 15.03.2026 u 10:00 (Šišanje) je otkazan. Za novi termin pozovite 011/1234567."
    /// </summary>
    public static string AppointmentCancellation(
        string clientName,
        string salonName,
        string date,
        string time,
        string serviceNames,
        string? reason,
        string? salonPhone)
    {
        var body = $"Obaveštenje: Vaš termin u {salonName} za {date} u {time} ({serviceNames}) je otkazan.";

        if (!string.IsNullOrWhiteSpace(reason))
        {
            body += $" Razlog: {reason}.";
        }

        if (!string.IsNullOrWhiteSpace(salonPhone))
        {
            body += $" Za novi termin pozovite {salonPhone}.";
        }

        return body;
    }
}
