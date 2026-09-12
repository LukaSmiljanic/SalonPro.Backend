using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using SalonPro.API.BackgroundServices;
using SalonPro.API.Filters;
using SalonPro.API.Middleware;
using SalonPro.Application;
using SalonPro.Infrastructure;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Infrastructure.OpenAi;
using SalonPro.Infrastructure.Persistence;
using SalonPro.Infrastructure.Seed;
using SalonPro.Infrastructure.Social;
using SalonPro.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SalonPro API",
        Version = "v1",
        Description = "REST API for SalonPro salon management platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<TenantHeaderOperationFilter>();
});

// Application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CompletePastAppointmentsJob>();
builder.Services.AddHostedService<AppointmentReminderJob>();
builder.Services.AddHostedService<SubscriptionExpirationJob>();
builder.Services.AddHostedService<SocialPostPublishJob>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Log OpenAI config status at startup (never log the key itself).
{
    var openAi = app.Configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new OpenAiSettings();
    var envKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey");
    var hasKey = !string.IsNullOrWhiteSpace(envKey) || !string.IsNullOrWhiteSpace(openAi.ApiKey);
    app.Logger.LogInformation(
        "OpenAI startup: Enabled={Enabled}, HasKey={HasKey}, GenerateImages={Images}",
        openAi.Enabled, hasKey, openAi.GenerateImages);
}

// ── Social media static files (public images for Instagram) ─────────────
var socialSettings = builder.Configuration.GetSection(SocialStorageSettings.SectionName).Get<SocialStorageSettings>()
    ?? new SocialStorageSettings();
var socialRoot = LocalMediaStorageService.GetPhysicalRoot(app.Environment, socialSettings);
try
{
    Directory.CreateDirectory(socialRoot);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not create social media folder at {Path}", socialRoot);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(socialRoot),
    RequestPath = "/media/social",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
    },
});

// ── Middleware pipeline ───────────────────────────────────────────────────
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

//app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SubscriptionCheckMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));
app.MapControllers();

// ── Database bootstrap (non-blocking — API listens while DB seed runs) ───
_ = Task.Run(async () =>
{
    try
    {
        await RunStartupBootstrapAsync(app);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Startup bootstrap failed");
    }
});

app.Run();

static async Task RunStartupBootstrapAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var schema = scope.ServiceProvider.GetRequiredService<ISocialSchemaBootstrapper>();
    var schemaResult = await schema.EnsureAsync();
    if (!schemaResult.Success)
        logger.LogWarning("Social schema bootstrap incomplete: {Steps}", string.Join("; ", schemaResult.Steps));

    try
    {
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        await DatabaseSeeder.SeedAsync(context, passwordService);

        await context.Database.ExecuteSqlRawAsync(
            "UPDATE Tenants SET Currency = 'RSD' WHERE Currency = 'EUR' OR Currency IS NULL");

        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Users', 'PasswordResetToken') IS NULL
            BEGIN
                ALTER TABLE Users ADD PasswordResetToken NVARCHAR(256) NULL;
                ALTER TABLE Users ADD PasswordResetTokenExpiry DATETIME2 NULL;
            END");

        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Tenants', 'SubscriptionExpiryWarningSentUtc') IS NULL
            BEGIN
                ALTER TABLE Tenants ADD SubscriptionExpiryWarningSentUtc DATETIME2 NULL;
            END");

        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Tenants', 'Plan') IS NULL
            BEGIN
                ALTER TABLE Tenants ADD Plan NVARCHAR(20) NOT NULL CONSTRAINT DF_Tenants_Plan DEFAULT 'Basic';
            END");

        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE Tenants SET Plan = 'Basic'
            WHERE Plan IS NULL OR LTRIM(RTRIM(Plan)) = ''");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database migration/seed");
    }
}
