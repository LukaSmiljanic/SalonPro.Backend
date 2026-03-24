using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IDateTimeService dateTimeService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _dateTimeService = dateTimeService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLower(), cancellationToken);

        if (existingUser != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Ova email adresa je već u upotrebi.") });

        // Check if tenant slug already exists
        var existingTenant = await _unitOfWork.Tenants.FirstOrDefaultAsync(
            t => t.Slug == request.TenantSlug.ToLower(), cancellationToken);

        if (existingTenant != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("TenantSlug", "Ovaj URL identifikator salona je već zauzet.") });

        // Generate email verification token
        var verificationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Create tenant with verification + trial subscription
        var now = _dateTimeService.UtcNow;
        var tenant = new Tenant
        {
            Name = request.TenantName,
            Slug = request.TenantSlug.ToLower(),
            Email = request.Email.ToLower(),
            IsActive = true,
            EmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = now.AddHours(48),
            IsTrialing = true,
            SubscriptionStartDate = now,
            SubscriptionEndDate = now.AddDays(30),
            CreatedAt = now
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);

        // Create admin user
        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://relaxed-ganache-48eccf.netlify.app";
                var verificationUrl = $"{baseUrl}/verify-email?token={verificationToken}";
                await _emailService.SendEmailVerificationAsync(request.Email, tenant.Name, verificationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
            }
        }, cancellationToken);

        // Return response WITHOUT tokens — user must verify email first
        return new AuthResponseDto
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = now,
            User = new AuthUserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Role = user.Role.ToString(),
                TenantId = tenant.Id.ToString(),
                TenantName = tenant.Name
            },
            RequiresEmailVerification = true
        };
    }
}
