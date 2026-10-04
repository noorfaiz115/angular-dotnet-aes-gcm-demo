namespace Demo.Api.Configuration;

public sealed class NoStoreMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        await next(context);
    }
}
