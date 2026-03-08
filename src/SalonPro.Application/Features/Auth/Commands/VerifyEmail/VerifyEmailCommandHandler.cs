using MediatR;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeService dateTimeService,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return new VerifyEmailResult(false, "Token nije validan.");

        var tenant = await _unitOfWork.Tenants.FirstOrDefaultAsync(
            t => t.EmailVerificationToken == request.Token, cancellationToken);

        if (tenant == null)
            return new VerifyEmailResult(false, "Token nije pronađen ili je već iskorišćen.");

        if (tenant.EmailVerified)
            return new VerifyEmailResult(true, "Email je već verifikovan. Možete se prijaviti.");

        if (tenant.EmailVerificationTokenExpiry.HasValue &&
            tenant.EmailVerificationTokenExpiry.Value < _dateTimeService.UtcNow)
            return new VerifyEmailResult(false, "Token je istekao. Registrujte se ponovo.");

        // Activate the tenant
        var now = _dateTimeService.UtcNow;
        tenant.EmailVerified = true;
        tenant.EmailVerificationToken = null;
        tenant.EmailVerificationTokenExpiry = null;

        // Start 30-day trial from verification moment
        tenant.IsTrialing = true;
        tenant.SubscriptionStartDate = now;
        tenant.SubscriptionEndDate = now.AddDays(30);

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} ({TenantName}) email verified. Trial started until {TrialEnd}.",
            tenant.Id, tenant.Name, tenant.SubscriptionEndDate);

        return new VerifyEmailResult(true, "Email je uspešno verifikovan! Vaš 30-dnevni trial period je počeo.");
    }
}
