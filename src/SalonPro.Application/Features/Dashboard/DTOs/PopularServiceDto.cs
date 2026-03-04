using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Dashboard.DTOs;

public record PopularServiceDto(
    string ServiceName,
    int BookingCount,
    ServiceCategoryType CategoryType,
    string CategoryColorHex
);
