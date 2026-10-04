using Demo.Gateway.Services;
namespace Demo.Gateway.Endpoints;

public static class GatewayEndpoints
{
    public static IEndpointRouteBuilder MapGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { service = "Demo.Gateway", status = "healthy" }));
        endpoints.MapGet("/api/health", (HttpContext context, ApiForwarder forwarder) => forwarder.ForwardAsync(context));
        foreach (var path in new[] { "/api/crypto/session", "/api/secure/echo", "/api/auth/register", "/api/auth/login", "/api/auth/me", "/api/auth/logout" })
            endpoints.MapPost(path, (HttpContext context, ApiForwarder forwarder) => forwarder.ForwardAsync(context));
        return endpoints;
    }
}
