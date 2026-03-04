using MediatR;
using SalonPro.Application.Features.Auth.DTOs;

namespace SalonPro.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
