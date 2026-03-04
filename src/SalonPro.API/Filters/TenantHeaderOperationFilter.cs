using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SalonPro.API.Filters;

/// <summary>
/// Adds the X-Tenant-Id header parameter to all Swagger operations.
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Tenant ID for multi-tenant operations",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }
}
