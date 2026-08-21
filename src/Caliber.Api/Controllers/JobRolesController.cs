using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/job-roles")]
public sealed class JobRolesController(RoleRequirementService roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await roles.ListRolesAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await roles.GetRoleAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roles.CreateRoleAsync(request, cancellationToken));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateJobRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roles.UpdateRoleAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await roles.DeleteRoleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/requirements")]
    public async Task<IActionResult> AddRequirement(
        int id,
        [FromBody] AddRoleRequirementRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roles.AddRequirementAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/apply")]
    public async Task<IActionResult> Apply(int id, CancellationToken cancellationToken) =>
        Ok(await roles.ApplyRoleRequirementsAsync(id, cancellationToken));
}
