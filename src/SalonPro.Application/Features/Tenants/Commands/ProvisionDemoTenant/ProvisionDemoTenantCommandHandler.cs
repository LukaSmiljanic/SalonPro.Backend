using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Tenants.Commands.ProvisionDemoTenant;

public class ProvisionDemoTenantCommandHandler : IRequestHandler<ProvisionDemoTenantCommand, ProvisionDemoTenantResult>
{
    public const int DemoTrialDays = 30;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProvisionDemoTenantCommandHandler> _logger;

    public ProvisionDemoTenantCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IEmailService emailService,
        IDateTimeService dateTimeService,
        IConfiguration configuration,
        ILogger<ProvisionDemoTenantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _emailService = emailService;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProvisionDemoTenantResult> Handle(ProvisionDemoTenantCommand request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var slugLower = request.TenantSlug.Trim().ToLowerInvariant();

        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == emailLower, cancellationToken);
        if (existingUser != null)
            throw new ValidationException("Korisnik sa ovom email adresom već postoji. Koristite drugi email ili postojeći nalog.");

        var existingTenantSlug = await _unitOfWork.Tenants.FirstOrDefaultAsync(t => t.Slug == slugLower, cancellationToken);
        if (existingTenantSlug != null)
            throw new ValidationException("Ovaj URL identifikator salona (slug) je već zauzet. Izaberite drugi (npr. salon-xyz-ruma).");

        var now = _dateTimeService.UtcNow;
        var tempPassword = string.IsNullOrWhiteSpace(request.Password)
            ? GenerateDemoPassword()
            : request.Password.Trim();
        var tenant = new Tenant
        {
            Name = request.TenantName.Trim(),
            Slug = slugLower,
            Email = emailLower,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            Country = string.IsNullOrWhiteSpace(request.City) ? null : "Serbia",
            IsActive = true,
            Plan = TenantPlanRules.Demo,
            EmailVerified = true,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            SubscriptionExpiryWarningSentUtc = null,
            IsTrialing = true,
            SubscriptionStartDate = now,
            SubscriptionEndDate = now.AddDays(30),
            CreatedAt = now
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = emailLower,
            PasswordHash = _passwordService.HashPassword(tempPassword),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = (_configuration["AppSettings:FrontendUrl"] ?? "https://salonpro.netlify.app").TrimEnd('/');
        var loginUrl = $"{baseUrl}/login";

        try
        {
            await _emailService.SendDemoAccessEmailAsync(
                emailLower,
                tenant.Name,
                loginUrl,
                emailLower,
                tempPassword,
                tenant.SubscriptionEndDate!.Value,
                DemoTrialDays,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Demo welcome email failed for {Email}; credentials are still valid.", emailLower);
        }

        return new ProvisionDemoTenantResult(
            tenant.Id,
            emailLower,
            tempPassword,
            tenant.SubscriptionEndDate.Value,
            DemoTrialDays);
    }

    private static string GenerateDemoPassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(14);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
