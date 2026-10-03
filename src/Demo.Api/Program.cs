var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { service = "Demo.Api", status = "healthy" }));
app.Run();
