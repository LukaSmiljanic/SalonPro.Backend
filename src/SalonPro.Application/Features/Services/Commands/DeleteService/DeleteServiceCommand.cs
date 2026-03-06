using MediatR;

namespace SalonPro.Application.Features.Services.Commands.DeleteService;

/// <summary>
/// Soft-deletes a service by setting IsActive = false.
/// </summary>
public record DeleteServiceCommand(Guid Id) : IRequest<Unit>;
