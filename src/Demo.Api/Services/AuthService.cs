using Demo.Api.Auth;
using Demo.Api.Models;
using Demo.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Demo.Api.Services;
public sealed class AuthService(IUserRepository users, ILoginSessionRepository logins) : IAuthService
{
    private readonly PasswordHasher<UserEntity> hasher = new();
    private readonly object gate = new();
    private readonly UserEntity dummy = CreateDummy();
    private DateTimeOffset window = DateTimeOffset.UtcNow;
    private int attempts;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static UserEntity CreateDummy()
    {
        var user = new UserEntity("dummy", "dummy", "dummy", "");
        return user with { PasswordHash = new PasswordHasher<UserEntity>().HashPassword(user, Convert.ToHexString(RandomNumberGenerator.GetBytes(32))) };
    }
    public AuthResult Handle(string operation, JsonElement payload)
    {
        lock (gate)
        {
            if (payload.ValueKind != JsonValueKind.Object) return new(false, "Expected a JSON object.");
            try
            {
                if (operation is "register" or "login")
                {
                    if (DateTimeOffset.UtcNow - window >= TimeSpan.FromMinutes(1)) { window = DateTimeOffset.UtcNow; attempts = 0; }
                    if (++attempts > 60) return new(false, "Too many attempts. Try again in a minute.");
                    var input = payload.Deserialize<Credentials>(JsonOptions);
                    if (input is null || !ValidEmail(input.Email) || input.Password is null || input.Password.Length < 12 || input.Password.Length > 128)
                        return new(false, "Use a valid email and a password between 12 and 128 characters.");
                    var email = input.Email!.Trim().ToLowerInvariant();
                    return operation == "register" ? Register(input, email) : LoginUser(input.Password, email);
                }
                var token = payload.Deserialize<TokenRequest>(JsonOptions)?.Token;
                if (token is null || token.Length != 64 || !token.All(Uri.IsHexDigit)) return new(false, "Please log in.");
                var tokenHash = HashToken(token);
                var login = logins.FindByTokenHash(tokenHash);
                if (login is null)
                { logins.Remove(tokenHash); return new(false, "Login expired. Please log in again."); }
                if (operation == "logout") { logins.Remove(tokenHash); return new(true, "Logged out."); }
                return new(true, "Authenticated profile loaded.", ToPublic(users.FindByEmail(login.Email)!));
            }
            catch (JsonException) { return new(false, "Invalid authentication payload."); }
        }
    }
    private AuthResult Register(Credentials input, string email)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 80) return new(false, "Name must be between 1 and 80 characters.");
        if (users.FindByEmail(email) is not null) return new(false, "Unable to register this email. Try logging in instead.");
        var user = new UserEntity(Guid.NewGuid().ToString("N"), name, email, "");
        user = user with { PasswordHash = hasher.HashPassword(user, input.Password!) };
        var added = users.TryAdd(user);
        if (added == AddUserResult.Duplicate) return new(false, "Unable to register this email. Try logging in instead.");
        if (added == AddUserResult.CapacityReached) return new(false, "Demo account capacity reached.");
        return new(true, "Registration successful. You can now log in.", ToPublic(user));
    }
    private AuthResult LoginUser(string password, string email)
    {
        var user = users.FindByEmail(email);
        var exists = user is not null;
        user ??= dummy;
        var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (!exists || verified == PasswordVerificationResult.Failed || user.LockedUntil > DateTimeOffset.UtcNow)
        {
            if (exists && user.LockedUntil <= DateTimeOffset.UtcNow)
            {
                user = user with { Failures = user.Failures + 1 };
                if (user.Failures >= 5) user = user with { Failures = 0, LockedUntil = DateTimeOffset.UtcNow.AddMinutes(1) };
                users.Update(user);
            }
            return new(false, "Invalid email or password, or account temporarily locked.");
        }
        user = user with { Failures = 0 };
        if (verified == PasswordVerificationResult.SuccessRehashNeeded) user = user with { PasswordHash = hasher.HashPassword(user, password) };
        users.Update(user);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        if (!logins.TryAdd(HashToken(token), new LoginSession(email, DateTimeOffset.UtcNow.AddMinutes(30)))) return new(false, "Demo login capacity reached.");
        return new(true, "Login successful.", ToPublic(user), token);
    }
    private static PublicUser ToPublic(UserEntity user) => new(user.Id, user.Name, user.Email);
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool ValidEmail(string? email) => email is not null && email.Trim().Length <= 254 &&
        MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim() && address.Host.Contains('.');
}
