using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SalonPro.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("[MediatR] Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("[MediatR] Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[MediatR] Error handling {RequestName} after {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
