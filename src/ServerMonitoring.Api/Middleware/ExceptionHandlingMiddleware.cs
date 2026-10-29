using System.Text.Json;
using ServerMonitoring.Application.Common;
using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await WriteAsync(context, exception.StatusCode, exception.Title, exception.Message, exception.Errors);
        }
        catch (AppException exception)
        {
            await WriteAsync(context, exception.StatusCode, exception.Title, exception.Message, null);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "خطای کنترل‌نشده در پردازش {Method} {Path}", context.Request.Method, context.Request.Path.Value);
            var message = environment.IsDevelopment() ? exception.Message : "خطای داخلی سرور";
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "خطای داخلی", message, null);
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string message,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Success = false,
            Title = title,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        }, JsonOptions);
    }
}
