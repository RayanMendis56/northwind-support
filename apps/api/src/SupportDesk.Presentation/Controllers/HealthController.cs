using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(SupportDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAsync(CancellationToken ct)
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "Unhealthy", database = "Disconnected" });
        }

        return Ok(new { status = "Healthy", database = "Connected" });
    }
}
