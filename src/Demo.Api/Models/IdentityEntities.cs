namespace Demo.Api.Models;

public sealed record UserEntity(string Id, string Name, string Email, string PasswordHash, int Failures = 0, DateTimeOffset LockedUntil = default);
public sealed record LoginSession(string Email, DateTimeOffset Expires);
