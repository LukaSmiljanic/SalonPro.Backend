using MediatR;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.ServiceCategories.Commands.CreateServiceCategory;

public record CreateServiceCategoryCommand(
    string Name,
    string? Description,
    string? ColorHex,
    ServiceCategoryType Type = ServiceCategoryType.Other
) : IRequest<Guid>;
