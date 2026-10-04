using Demo.Api.Repositories;
using Demo.Api.Services;
using Demo.Api.Transport;
using Microsoft.AspNetCore.RateLimiting;
namespace Demo.Api.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddDemoApi(this IServiceCollection services)
    {
        services.AddControllers();
        // Singleton lifetimes preserve shared demo data, lockout state and crypto sessions.
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<ILoginSessionRepository, InMemoryLoginSessionRepository>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IEncryptedSessionService, SessionStore>();
        services.AddRateLimiter(options => options.AddFixedWindowLimiter("bootstrap", limiter =>
        {
            limiter.PermitLimit = 30;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        }));
        return services;
    }
}
