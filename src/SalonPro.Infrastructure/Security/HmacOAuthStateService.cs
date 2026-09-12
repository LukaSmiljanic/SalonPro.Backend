using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Security;

public class HmacOAuthStateService : IOAuthStateService
{
    private readonly byte[] _key;

    public HmacOAuthStateService(IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret nije konfigurisan.");
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string CreateState(Guid tenantId)
    {
        var id = tenantId.ToString("N");
        return $"{id}.{Sign(id)}";
    }

    public Guid ParseState(string state)
    {
        var dot = state.LastIndexOf('.');
        if (dot <= 0)
            throw new InvalidOperationException("Neispravan OAuth state. Pokušajte ponovo.");

        var id = state[..dot];
        var sig = state[(dot + 1)..];

        if (!FixedTimeEquals(Sign(id), sig))
            throw new InvalidOperationException("OAuth sesija je istekla. Kliknite Poveži Instagram ponovo.");

        return Guid.Parse(id);
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
