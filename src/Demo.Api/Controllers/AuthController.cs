using Demo.Api.Services;
using Demo.Api.Transport;
using Microsoft.AspNetCore.Mvc;
namespace Demo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IEncryptedSessionService sessions, IAuthService auth) : EncryptedControllerBase
{
    [HttpPost("register")]
    public IActionResult Register(Envelope envelope) => Handle("register", envelope);
    [HttpPost("login")]
    public IActionResult Login(Envelope envelope) => Handle("login", envelope);
    [HttpPost("me")]
    public IActionResult Profile(Envelope envelope) => Handle("me", envelope);
    [HttpPost("logout")]
    public IActionResult Logout(Envelope envelope) => Handle("logout", envelope);
    private IActionResult Handle(string operation, Envelope envelope) =>
        Execute(() => sessions.Process(envelope, "/api/auth/" + operation, payload => auth.Handle(operation, payload)));
}
