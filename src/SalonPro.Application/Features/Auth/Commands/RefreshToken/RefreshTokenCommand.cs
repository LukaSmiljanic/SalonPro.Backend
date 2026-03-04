using MediatR;
using SalonPro.Application.Features.Auth.DTOs;

namespace SalonPro.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponseDto>;
