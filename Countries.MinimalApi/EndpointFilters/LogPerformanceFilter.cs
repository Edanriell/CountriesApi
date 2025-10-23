using System.Diagnostics;

namespace Countries.MinimalApi.EndpointFilters;

public class LogPerformanceFilter : IEndpointFilter
{
    private readonly ILogger<LogPerformanceFilter> _logger;

    public LogPerformanceFilter(ILogger<LogPerformanceFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _logger.LogInformation("GET /longrunning endpoint getting executed");
        var startTime = Stopwatch.GetTimestamp();
        var result = await next(context);
        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        _logger.LogInformation($"GET /longrunning endpoint took {elapsedTime.TotalSeconds} to execute");
        return result;
    }
}