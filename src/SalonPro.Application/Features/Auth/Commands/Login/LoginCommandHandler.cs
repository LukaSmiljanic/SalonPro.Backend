using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeService _dateTimeService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.Tenant)
            .Where(u => u.Email == request.Email.ToLower() && u.IsActive)
            .Where(u => !request.TenantId.HasValue || u.TenantId == request.TenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Pogrešan email ili lozinka.");

        var tenant = user.Tenant;

        // Block login if email is not verified
        if (tenant != null && !tenant.EmailVerified)
            throw new UnauthorizedException("Email adresa nije verifikovana. Proverite inbox za aktivacioni link.");

        // Block login if subscription has expired
        if (tenant != null && !tenant.HasActiveSubscription)
            throw new UnauthorizedException("Vaša pretplata je istekla. Kontaktirajte podršku za produženje.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = _dateTimeService.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Determine subscription status
        string? subscriptionStatus = null;
        if (tenant != null)
        {
            if (tenant.IsTrialing && tenant.HasActiveSubscription)
                subscriptionStatus = "Trial";
            else if (tenant.HasActiveSubscription)
                subscriptionStatus = "Active";
            else
                subscriptionStatus = "Expired";
        }

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60),
            User = new AuthUserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Role = user.Role.ToString(),
                TenantId = user.TenantId.ToString(),
                TenantName = tenant?.Name ?? string.Empty
            },
            SubscriptionStatus = subscriptionStatus,
            SubscriptionEndDate = tenant?.SubscriptionEndDate
        };
    }
}
