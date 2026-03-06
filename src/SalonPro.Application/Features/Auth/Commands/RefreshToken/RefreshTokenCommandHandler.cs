using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeService _dateTimeService;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate the refresh token and extract user id
        var userId = _jwtTokenService.ValidateRefreshToken(request.RefreshToken, null!);

        if (userId == null)
            throw new UnauthorizedException("Invalid refresh token.");

        var user = await _unitOfWork.Users.Query()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(
            u => u.Id == userId && u.RefreshToken == request.RefreshToken && u.IsActive, cancellationToken);

        if (user == null || user.RefreshTokenExpiry <= _dateTimeService.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = _dateTimeService.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60),
            User = new AuthUserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Role = user.Role.ToString(),
                TenantId = user.TenantId.ToString(),
                TenantName = user.Tenant?.Name ?? string.Empty
            }
        };

        return response;
    }
}
