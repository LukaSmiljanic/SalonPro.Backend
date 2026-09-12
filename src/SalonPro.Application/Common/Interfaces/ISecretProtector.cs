namespace SalonPro.Application.Common.Interfaces;

public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
