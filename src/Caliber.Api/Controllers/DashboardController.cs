using Caliber.Api.Abstractions;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ReadinessService readiness, ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await readiness.GetDashboardAsync(user, cancellationToken));
}
