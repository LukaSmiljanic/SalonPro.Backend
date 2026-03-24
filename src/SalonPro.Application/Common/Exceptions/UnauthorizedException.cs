namespace SalonPro.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Neautorizovan pristup.") { }
    public UnauthorizedException(string message) : base(message) { }
}
