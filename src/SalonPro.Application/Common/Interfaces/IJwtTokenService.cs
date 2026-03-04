using SalonPro.Domain.Entities;

namespace SalonPro.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token, User user);
}
