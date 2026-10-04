namespace Demo.Api.Auth;

public sealed record Credentials(string? Name, string? Email, string? Password);
public sealed record TokenRequest(string? Token);
public sealed record PublicUser(string Id, string Name, string Email);
public sealed record AuthResult(bool Ok, string Message, PublicUser? User = null, string? Token = null);
