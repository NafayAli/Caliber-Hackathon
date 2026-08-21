using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(SkillService skills) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await skills.ListCatalogueAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await skills.GetCatalogueItemAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSkillRequest request,
        CancellationToken cancellationToken) =>
        Ok(await skills.CreateAsync(request, cancellationToken));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateSkillRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await skills.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await skills.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
