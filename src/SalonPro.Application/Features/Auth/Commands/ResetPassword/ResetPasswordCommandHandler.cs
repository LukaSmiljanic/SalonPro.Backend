using MediatR;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return new ResetPasswordResult(false, "Token nije validan.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return new ResetPasswordResult(false, "Lozinka mora imati najmanje 6 karaktera.");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null)
            return new ResetPasswordResult(false, "Link za resetovanje lozinke nije validan ili je već iskorišćen.");

        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return new ResetPasswordResult(false, "Link za resetovanje lozinke je istekao. Zatražite novi.");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successful for user {UserId}", user.Id);

        return new ResetPasswordResult(true, "Lozinka je uspešno promenjena. Sada se možete prijaviti.");
    }
}
