using MediatR;

namespace SalonPro.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
