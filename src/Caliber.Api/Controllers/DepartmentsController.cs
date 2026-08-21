using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(RoleRequirementService roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await roles.ListDepartmentsAsync(cancellationToken));
}
