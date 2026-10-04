using Demo.Api.Transport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
namespace Demo.Api.Controllers;

[ApiController]
public sealed class CryptoController(IEncryptedSessionService sessions) : EncryptedControllerBase
{
    [HttpPost("api/crypto/session")]
    [EnableRateLimiting("bootstrap")]
    public IActionResult CreateSession(SessionRequest request)
    {
        try { return Ok(sessions.Create(request)); }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        { return BadRequest(new { error = "Invalid P-256 public key." }); }
        catch (InvalidOperationException) { return StatusCode(503); }
    }
    [HttpPost("api/secure/echo")]
    public IActionResult Echo(Envelope envelope) => Execute(() => sessions.Echo(envelope, "/api/secure/echo"));
}
