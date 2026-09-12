using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;
using SalonPro.Infrastructure.Email;
using SalonPro.Infrastructure.Persistence;
using SalonPro.Infrastructure.Persistence.Interceptors;
using SalonPro.Infrastructure.Services;
using SalonPro.Infrastructure.Sms;
using Microsoft.AspNetCore.DataProtection;
using SalonPro.Infrastructure.Meta;
using SalonPro.Infrastructure.OpenAi;
using SalonPro.Infrastructure.Security;
using SalonPro.Infrastructure.Social;
using SalonPro.Infrastructure.Storage;

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

        // ── Email ────────────────────────────────────────────────────
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, MailKitEmailService>();

        // ── SMS (Infobip) ─────────────────────────────────────────────
        services.Configure<SmsSettings>(configuration.GetSection(SmsSettings.SectionName));
        services.AddHttpClient("Infobip");
        services.AddScoped<ISmsService, InfobipSmsService>();

        // ── Repository / UnitOfWork ──────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ISocialSchemaBootstrapper, SocialSchemaBootstrapper>();
        services.AddScoped<ISocialDatabaseDiagnostics, SocialDatabaseDiagnostics>();

        // ── Social / OpenAI / Instagram ───────────────────────────────
        var dpBuilder = services.AddDataProtection().SetApplicationName("SalonPro");
        foreach (var dpKeysDir in GetDataProtectionKeyDirectories())
        {
            try
            {
                Directory.CreateDirectory(dpKeysDir);
                dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir));
                break;
            }
            catch
            {
                // Monster shared hosting may block ../data — try next path.
            }
        }

        services.AddSingleton<IOAuthStateService, HmacOAuthStateService>();
        services.Configure<OpenAiSettings>(configuration.GetSection(OpenAiSettings.SectionName));
        services.PostConfigure<OpenAiSettings>(options =>
        {
            var envKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                options.ApiKey = envKey.Trim();
                options.KeySource = "environment";
            }
            else if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = options.ApiKey.Trim();
                options.KeySource = "appsettings";
            }
            else
            {
                options.KeySource = "none";
            }
        });
        services.Configure<MetaSettings>(configuration.GetSection(MetaSettings.SectionName));
        services.Configure<SocialStorageSettings>(configuration.GetSection(SocialStorageSettings.SectionName));
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddHttpClient<OpenAiService>(c => c.Timeout = TimeSpan.FromMinutes(6));
        services.AddHttpClient("OpenAiImageFetch", c =>
        {
            c.Timeout = TimeSpan.FromMinutes(3);
            c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SalonPro/1.0");
        });
        services.AddScoped<IOpenAiService>(sp => sp.GetRequiredService<OpenAiService>());
        services.AddHttpClient("MetaGraph", c => c.BaseAddress = new Uri("https://graph.facebook.com/"));
        services.AddSingleton<IInstagramOAuthQueue, InstagramOAuthCompletionQueue>();
        services.AddScoped<IInstagramService, InstagramGraphService>();
        services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
        services.AddScoped<ISocialPublishService, SocialPublishService>();
        services.AddScoped<SalonSocialContentGenerator>();
        services.AddScoped<ISocialContentGenerator, OpenAiSocialContentGenerator>();

        services.AddSingleton<SocialImageGenerationQueue>();
        services.AddSingleton<ISocialImageQueue>(sp => sp.GetRequiredService<SocialImageGenerationQueue>());

        // ── JWT Authentication ───────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret nije konfigurisan.");

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

    private static IEnumerable<string> GetDataProtectionKeyDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "..", "data", "dp-keys");
        yield return Path.Combine(AppContext.BaseDirectory, "data", "dp-keys");
        yield return Path.Combine(Path.GetTempPath(), "SalonPro", "dp-keys");
    }
}
