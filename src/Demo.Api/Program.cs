using Microsoft.AspNetCore.RateLimiting;
using Demo.Api.Transport;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32768);
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("bootstrap", limiter =>
{
    limiter.PermitLimit = 30;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
}));
var app = builder.Build();
app.UseRateLimiter();
app.Use(async (context, next) => { context.Response.Headers.CacheControl = "no-store"; await next(context); });
app.MapGet("/api/health", () => Results.Ok(new { service = "Demo.Api", status = "healthy" }));
app.MapPost("/api/crypto/session", (SessionRequest request, SessionStore sessions) =>
{
    try { return Results.Ok(sessions.Create(request)); }
    catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
    { return Results.BadRequest(new { error = "Invalid P-256 public key." }); }
    catch (InvalidOperationException) { return Results.StatusCode(503); }
}).RequireRateLimiting("bootstrap");
app.MapPost("/api/secure/echo", (Envelope envelope, SessionStore sessions) =>
{
    try { return Results.Ok(sessions.Echo(envelope, "/api/secure/echo")); }
    catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
    { return Results.BadRequest(new { error = "Invalid encrypted envelope." }); }
    catch (InvalidOperationException ex) { return Results.Json(new { error = ex.Message }, statusCode: 409); }
});
app.Run();
