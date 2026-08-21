using Caliber.Api.Abstractions;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/expirations")]
public sealed class ExpirationsController(ReadinessService readiness, ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await readiness.GetExpirationsAsync(user, cancellationToken));
}
