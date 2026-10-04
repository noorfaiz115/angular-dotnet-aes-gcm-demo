using Microsoft.AspNetCore.Mvc;
namespace Demo.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("api/health")]
    public IActionResult Get() => Ok(new { service = "Demo.Api", status = "healthy" });
}
