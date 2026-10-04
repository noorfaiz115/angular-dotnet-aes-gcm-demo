using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
namespace Demo.Api.Controllers;

// Converts only transport failures to plaintext protocol errors.
public abstract class EncryptedControllerBase : ControllerBase
{
    protected IActionResult Execute(Func<object> action)
    {
        try { return Ok(action()); }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        { return BadRequest(new { error = "Invalid encrypted envelope." }); }
        catch (InvalidOperationException ex) { return StatusCode(409, new { error = ex.Message }); }
    }
}
