using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SalonPro.Application.Common.Exceptions;

namespace SalonPro.API.Filters;

/// <summary>
/// Exception filter attribute that handles known application exceptions and converts them
/// to appropriate HTTP responses. Acts as a backup to <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;

    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        HandleException(context);
        base.OnException(context);
    }

    private void HandleException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case Application.Common.Exceptions.ValidationException validationException:
                HandleValidationException(context, validationException);
                break;
            case NotFoundException notFoundException:
                HandleNotFoundException(context, notFoundException);
                break;
            case UnauthorizedException unauthorizedException:
                HandleUnauthorizedException(context, unauthorizedException);
                break;
            case ForbiddenAccessException forbiddenException:
                HandleForbiddenAccessException(context, forbiddenException);
                break;
            default:
                HandleUnknownException(context);
                break;
        }
    }

    private static void HandleValidationException(ExceptionContext context, Application.Common.Exceptions.ValidationException exception)
    {
        var details = new ValidationProblemDetails(exception.Errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Došlo je do jedne ili više grešaka pri validaciji.",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message
        };

        context.Result = new BadRequestObjectResult(details);
        context.ExceptionHandled = true;
    }

    private static void HandleNotFoundException(ExceptionContext context, NotFoundException exception)
    {
        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Traženi resurs nije pronađen.",
            Status = StatusCodes.Status404NotFound,
            Detail = exception.Message
        };

        context.Result = new NotFoundObjectResult(details);
        context.ExceptionHandled = true;
    }

    private static void HandleUnauthorizedException(ExceptionContext context, UnauthorizedException exception)
    {
        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Neautorizovan pristup.",
            Status = StatusCodes.Status401Unauthorized,
            Detail = exception.Message
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
        context.ExceptionHandled = true;
    }

    private static void HandleForbiddenAccessException(ExceptionContext context, ForbiddenAccessException exception)
    {
        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Title = "Zabranjen pristup.",
            Status = StatusCodes.Status403Forbidden,
            Detail = exception.Message
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
        context.ExceptionHandled = true;
    }

    private void HandleUnknownException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "An unhandled exception occurred.");

        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Došlo je do greške prilikom obrade zahteva.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Došlo je do neočekivane greške. Molimo pokušajte ponovo kasnije."
        };

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}
