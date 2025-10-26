using Microsoft.AspNetCore.Builder;

namespace BuildingBlocks.Observability;

public static class CorrelationExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
