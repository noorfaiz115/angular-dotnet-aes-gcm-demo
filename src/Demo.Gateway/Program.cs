using Demo.Gateway.Endpoints;
using Demo.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32768);
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5200");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<ApiForwarder>();

var app = builder.Build();
app.MapGatewayEndpoints();
app.Run();
