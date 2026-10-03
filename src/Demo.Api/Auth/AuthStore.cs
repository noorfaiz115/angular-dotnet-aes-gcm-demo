using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Demo.Api.Auth;

public sealed record Credentials(string? Name, string? Email, string? Password);
public sealed record TokenRequest(string? Token);
public sealed record PublicUser(string Id, string Name, string Email);
public sealed record AuthResult(bool Ok, string Message, PublicUser? User = null, string? Token = null);

// Learning-only bounded in-memory identity store. Restarting the API clears users and logins.
public sealed class AuthStore
{
    private sealed class User(PublicUser profile)
    {
        public PublicUser Profile = profile;
        public string PasswordHash = "";
        public int Failures;
        public DateTimeOffset LockedUntil;
    }
    private sealed record Login(string Email, DateTimeOffset Expires);
    private readonly Dictionary<string, User> users = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Login> logins = new(StringComparer.Ordinal);
    private readonly PasswordHasher<User> hasher = new();
    private readonly object gate = new();
    private readonly User dummy = new(new("dummy", "dummy", "dummy"));
    private DateTimeOffset window = DateTimeOffset.UtcNow;
    private int attempts;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuthStore() { dummy.PasswordHash = hasher.HashPassword(dummy, Convert.ToHexString(RandomNumberGenerator.GetBytes(32))); }
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
                if (!logins.TryGetValue(tokenHash, out var login) || login.Expires <= DateTimeOffset.UtcNow)
                { logins.Remove(tokenHash); return new(false, "Login expired. Please log in again."); }
                if (operation == "logout") { logins.Remove(tokenHash); return new(true, "Logged out."); }
                return new(true, "Authenticated profile loaded.", users[login.Email].Profile);
            }
            catch (JsonException) { return new(false, "Invalid authentication payload."); }
        }
    }
    private AuthResult Register(Credentials input, string email)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 80) return new(false, "Name must be between 1 and 80 characters.");
        if (users.ContainsKey(email)) return new(false, "Unable to register this email. Try logging in instead.");
        if (users.Count >= 1000) return new(false, "Demo account capacity reached.");
        var user = new User(new(Guid.NewGuid().ToString("N"), name, email));
        user.PasswordHash = hasher.HashPassword(user, input.Password!);
        users.Add(email, user);
        return new(true, "Registration successful. You can now log in.", user.Profile);
    }
    private AuthResult LoginUser(string password, string email)
    {
        var exists = users.TryGetValue(email, out var user);
        user ??= dummy;
        var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (!exists || verified == PasswordVerificationResult.Failed || user.LockedUntil > DateTimeOffset.UtcNow)
        {
            if (exists && user.LockedUntil <= DateTimeOffset.UtcNow && ++user.Failures >= 5)
            { user.Failures = 0; user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(1); }
            return new(false, "Invalid email or password, or account temporarily locked.");
        }
        user.Failures = 0;
        if (verified == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, password);
        foreach (var key in logins.Where(x => x.Value.Expires <= DateTimeOffset.UtcNow).Select(x => x.Key).ToArray()) logins.Remove(key);
        if (logins.Count >= 1000) return new(false, "Demo login capacity reached.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        logins.Add(HashToken(token), new(email, DateTimeOffset.UtcNow.AddMinutes(30)));
        return new(true, "Login successful.", user.Profile, token);
    }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool ValidEmail(string? email) => email is not null && email.Trim().Length <= 254 &&
        MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim() && address.Host.Contains('.');
}
