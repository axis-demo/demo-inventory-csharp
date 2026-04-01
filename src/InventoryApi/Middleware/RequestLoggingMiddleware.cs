using System.Diagnostics;

namespace InventoryApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N")[..8];

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[{CorrelationId}] {Method} {Path}{Query} started",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var level = context.Response.StatusCode >= 500
                ? LogLevel.Error
                : context.Response.StatusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

            _logger.Log(
                level,
                "[{CorrelationId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
