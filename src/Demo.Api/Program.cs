using Demo.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32768);
builder.Services.AddDemoApi();

var app = builder.Build();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<NoStoreMiddleware>();
app.MapControllers();
app.Run();
