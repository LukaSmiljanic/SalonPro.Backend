using MediatR;
using SalonPro.Application.Features.Auth.DTOs;

namespace SalonPro.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string TenantName,
    string TenantSlug,
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<AuthResponseDto>;
