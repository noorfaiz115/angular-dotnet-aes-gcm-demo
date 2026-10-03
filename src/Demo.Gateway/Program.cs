var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5200");
    client.Timeout = TimeSpan.FromSeconds(10);
});
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { service = "Demo.Gateway", status = "healthy" }));
app.MapGet("/api/health", async (IHttpClientFactory factory) =>
{
    try
    {
        using var response = await factory.CreateClient("api").GetAsync("/api/health");
        return Results.Content(await response.Content.ReadAsStringAsync(), "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("Backend API is unavailable.", statusCode: 502);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Backend API timed out.", statusCode: 504);
    }
});
app.Run();
