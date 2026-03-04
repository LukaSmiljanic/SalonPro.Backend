using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;
using SalonPro.Infrastructure.Persistence;
using SalonPro.Infrastructure.Persistence.Interceptors;
using SalonPro.Infrastructure.Services;

namespace SalonPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Interceptors (must be registered before DbContext) ─────
        services.AddScoped<AuditableEntityInterceptor>();

        // ── Database ────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(
                    typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // ── HttpContextAccessor (needed by CurrentUserService) ──────
        services.AddHttpContextAccessor();

        // ── Services ────────────────────────────────────────────────
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        // ── Repository / UnitOfWork ──────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ── JWT Authentication ───────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero,
            };
        });

        // ── Authorization ────────────────────────────────────────────
        services.AddAuthorization(options =>
        {
            options.AddPolicy("TenantAdmin", policy =>
                policy.RequireRole("TenantAdmin", "SuperAdmin"));

            options.AddPolicy("Manager", policy =>
                policy.RequireRole("TenantAdmin", "SuperAdmin", "Manager"));

            options.AddPolicy("Staff", policy =>
                policy.RequireRole("TenantAdmin", "SuperAdmin", "Manager", "Staff", "Receptionist"));
        });

        return services;
    }
}
