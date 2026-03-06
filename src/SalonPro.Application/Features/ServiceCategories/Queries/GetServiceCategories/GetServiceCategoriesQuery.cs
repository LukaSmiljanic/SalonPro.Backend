using MediatR;
using SalonPro.Application.Features.ServiceCategories.DTOs;

namespace SalonPro.Application.Features.ServiceCategories.Queries.GetServiceCategories;

public record GetServiceCategoriesQuery(bool IncludeInactive = false) : IRequest<List<ServiceCategoryDto>>;
