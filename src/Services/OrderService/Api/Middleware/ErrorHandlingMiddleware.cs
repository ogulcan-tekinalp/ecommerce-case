using System.Net;
using FluentValidation;

namespace OrderService.Api.Middleware;

public class ErrorHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        try { await next(ctx); }
        catch (ValidationException ve)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";
            var errors = ve.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
            await ctx.Response.WriteAsJsonAsync(new { title = "Validation failed", errors });
        }
    }
}
