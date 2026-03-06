using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.ServiceCategories.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.ServiceCategories.Queries.GetServiceCategories;

public class GetServiceCategoriesQueryHandler : IRequestHandler<GetServiceCategoriesQuery, List<ServiceCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServiceCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceCategoryDto>> Handle(GetServiceCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.ServiceCategories.Query()
            .Include(c => c.Services)
            .AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(c => new ServiceCategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.ColorHex ?? c.Color,
            c.Type,
            c.IsActive,
            c.Services?.Count ?? 0
        )).ToList();
    }
}
