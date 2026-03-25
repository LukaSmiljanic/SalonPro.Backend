using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always return success to prevent email enumeration
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            _logger.LogInformation("Password reset requested for unknown email: {Email}", request.Email);
            return Unit.Value; // Silent — don't reveal that email doesn't exist
        }

        // Generate secure token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour validity
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var baseUrl = (_configuration["AppSettings:FrontendUrl"] ?? "https://salonpro.netlify.app").TrimEnd('/');
                var resetUrl = $"{baseUrl}/reset-password?token={token}";
                await _emailService.SendPasswordResetAsync(user.Email, resetUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }, cancellationToken);

        return Unit.Value;
    }
}
