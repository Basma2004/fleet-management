using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Fleet.Shared.Observability.Middleware;

/// <summary>
/// Intercepts incoming HTTP requests to extract or generate an X-Correlation-ID header,
/// registers it in Serilog's LogContext, and echoes it back in the response headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CorrelationContext correlationContext)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        correlationContext.CorrelationId = correlationId;

        // Echo the correlation ID back to client in the response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push to Serilog ambient LogContext so all log lines include {CorrelationId}
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}