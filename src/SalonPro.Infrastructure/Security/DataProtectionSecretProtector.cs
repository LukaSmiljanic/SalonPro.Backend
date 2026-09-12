using Microsoft.AspNetCore.DataProtection;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Security;

public class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SalonPro.Social.Tokens");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);
    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
