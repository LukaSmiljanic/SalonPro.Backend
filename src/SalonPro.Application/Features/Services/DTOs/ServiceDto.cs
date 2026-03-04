namespace SalonPro.Application.Features.Services.DTOs;

public record ServiceDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive
);
