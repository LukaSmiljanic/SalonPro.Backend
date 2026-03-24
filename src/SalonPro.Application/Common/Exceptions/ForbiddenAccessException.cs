namespace SalonPro.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Pristup odbijen.") { }
    public ForbiddenAccessException(string message) : base(message) { }
}
