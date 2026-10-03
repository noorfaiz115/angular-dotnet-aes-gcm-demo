var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32768);
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5200");
    client.Timeout = TimeSpan.FromSeconds(10);
});
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { service = "Demo.Gateway", status = "healthy" }));
app.MapMethods("/api/{**path}", new[] { "GET", "POST" }, async (HttpContext context, IHttpClientFactory factory) =>
{
    var path = context.Request.Path.Value;
    var allowed = (context.Request.Method == "GET" && path == "/api/health") ||
        (context.Request.Method == "POST" && (path == "/api/crypto/session" || path == "/api/secure/echo" || path == "/api/auth/register" || path == "/api/auth/login" || path == "/api/auth/me" || path == "/api/auth/logout"));
    if (!allowed) { context.Response.StatusCode = 404; return; }
    try
    {
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), path);
        if (context.Request.Method == "POST")
        {
            request.Content = new StreamContent(context.Request.Body);
            request.Content.Headers.ContentType = new("application/json");
        }
        using var response = await factory.CreateClient("api").SendAsync(request, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        context.Response.Headers.CacheControl = "no-store";
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
    catch (HttpRequestException) { context.Response.StatusCode = 502; }
    catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested) { context.Response.StatusCode = 504; }
});
app.Run();
