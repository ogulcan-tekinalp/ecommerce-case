using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Threading.Tasks;

namespace BuildingBlocks.Observability;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var vals) && !string.IsNullOrWhiteSpace(vals.FirstOrDefault()))
        {
            CorrelationContext.Set(vals.First());
        }
        else
        {
            CorrelationContext.Set(Guid.NewGuid().ToString());
            context.Response.Headers[HeaderName] = CorrelationContext.Id;
        }

        using (LogContext.PushProperty("CorrelationId", CorrelationContext.Id))
        {
            // Also expose to downstream via Items
            context.Items[HeaderName] = CorrelationContext.Id;
            await _next(context);
        }
    }
}
