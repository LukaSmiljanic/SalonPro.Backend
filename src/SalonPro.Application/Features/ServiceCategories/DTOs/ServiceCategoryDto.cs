using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.ServiceCategories.DTOs;

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? ColorHex,
    ServiceCategoryType Type,
    bool IsActive,
    int ServiceCount
);
