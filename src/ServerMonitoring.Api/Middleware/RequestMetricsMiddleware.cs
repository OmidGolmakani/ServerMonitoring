using System.Diagnostics;
using ServerMonitoring.Application.Dtos;
using ServerMonitoring.Application.Interfaces;

namespace ServerMonitoring.Api.Middleware;

public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        if (IsSelfMonitoring(context.Request.Path))
        {
            await next(context);
            return;
        }

        var instrumentation = context.RequestServices.GetRequiredService<IRequestInstrumentation>();
        var monitoring = context.RequestServices.GetRequiredService<IMonitoringService>();
        var executionId = instrumentation.Begin();
        var started = Stopwatch.GetTimestamp();
        var originalBody = context.Response.Body;
        var countingBody = new CountingResponseStream(originalBody);
        context.Response.Body = countingBody;

        try
        {
            await next(context);
        }
        finally
        {
            var usage = instrumentation.Complete(executionId);
            monitoring.RecordRequest(new NewRequestObservationDto
            {
                Route = RouteNameResolver.Resolve(context),
                HttpMethod = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                CpuMilliseconds = usage.CpuMilliseconds,
                AllocatedBytes = usage.AllocatedBytes,
                ResponseBytes = countingBody.WrittenBytes,
                OccurredAtUtc = DateTime.UtcNow
            });
            context.Response.Body = originalBody;
        }
    }

    private static bool IsSelfMonitoring(PathString path) =>
        path.StartsWithSegments("/api/monitoring", StringComparison.OrdinalIgnoreCase);
}
