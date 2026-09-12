using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;

namespace SalonPro.API.Middleware;

/// <summary>
/// Global exception handling middleware that converts application exceptions
/// to RFC 7807 Problem Details JSON responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, configuration);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/api/social/instagram/callback", StringComparison.OrdinalIgnoreCase)
            && !context.Response.HasStarted)
        {
            var frontend = configuration["AppSettings:FrontendUrl"]?.TrimEnd('/') ?? "/";
            var reason = exception is InvalidOperationException ioe ? ioe.Message : "Neočekivana greška na serveru.";
            context.Response.Redirect($"{frontend}/marketing?instagram=error&reason={Uri.EscapeDataString(reason)}");
            return;
        }
        var (statusCode, title, detail, errors) = exception switch
        {
            Application.Common.Exceptions.ValidationException validationEx =>
                (HttpStatusCode.BadRequest,
                 "Došlo je do greške pri validaciji.",
                 validationEx.Message,
                 (IDictionary<string, string[]>?)validationEx.Errors),

            NotFoundException notFoundEx =>
                (HttpStatusCode.NotFound,
                 "Traženi resurs nije pronađen.",
                 notFoundEx.Message,
                 (IDictionary<string, string[]>?)null),

            UnauthorizedException unauthorizedEx =>
                (HttpStatusCode.Unauthorized,
                 "Neautorizovan pristup.",
                 unauthorizedEx.Message,
                 (IDictionary<string, string[]>?)null),

            ForbiddenAccessException forbiddenEx =>
                (HttpStatusCode.Forbidden,
                 "Pristup zabranjen.",
                 forbiddenEx.Message,
                 (IDictionary<string, string[]>?)null),

            InvalidOperationException invalidOpEx =>
                (HttpStatusCode.BadRequest,
                 "Zahtev nije mogao biti obraden.",
                 invalidOpEx.Message,
                 (IDictionary<string, string[]>?)null),

            HttpRequestException httpEx =>
                (HttpStatusCode.BadGateway,
                 "Spoljni servis nije dostupan.",
                 httpEx.Message,
                 (IDictionary<string, string[]>?)null),

            _ when exception is DbUpdateException dbEx =>
                (HttpStatusCode.ServiceUnavailable,
                 "Greška baze podataka.",
                 DescribeDatabaseError(dbEx),
                 (IDictionary<string, string[]>?)null),

            Microsoft.Data.SqlClient.SqlException sqlEx =>
                (HttpStatusCode.ServiceUnavailable,
                 "Greška baze podataka.",
                 DescribeDatabaseError(sqlEx),
                 (IDictionary<string, string[]>?)null),

            _ => LogAndReturnInternalServerError(exception)
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = BuildProblemDetails(statusCode, title, detail, errors);
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }

    private static string DescribeDatabaseError(Exception exception)
    {
        var root = exception;
        while (root.InnerException != null)
            root = root.InnerException;

        var sql = root.Message;
        if (sql.Contains("SocialPosts", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("TenantInstagramAccounts", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return $"Nedostaju tabele za marketing modul. U Monster SQL panelu pokrenite skriptu scripts/social-marketing-schema.sql. Detalj: {sql}";
        }

        return $"Šema baze nije usklađena. Detalj: {sql}";
    }

    private (HttpStatusCode, string, string, IDictionary<string, string[]>?) LogAndReturnInternalServerError(Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        return (
            HttpStatusCode.InternalServerError,
            "Došlo je do greške prilikom obrade zahteva.",
            "Neočekivana greška. Pokušajte ponovo.",
            null
        );
    }

    private static object BuildProblemDetails(
        HttpStatusCode statusCode,
        string title,
        string detail,
        IDictionary<string, string[]>? errors)
    {
        var type = statusCode switch
        {
            HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        if (errors != null)
        {
            return new
            {
                type,
                title,
                status = (int)statusCode,
                detail,
                errors
            };
        }

        return new
        {
            type,
            title,
            status = (int)statusCode,
            detail
        };
    }
}
