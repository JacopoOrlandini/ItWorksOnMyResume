using System.Net;
using System.Text.Json;

namespace IWOMR.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new
            {
                error     = "An unexpected error occurred.",
                requestId = ctx.TraceIdentifier
            });

            await ctx.Response.WriteAsync(body);
        }
    }
}
