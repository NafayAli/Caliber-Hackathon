using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/training-programs")]
public sealed class TrainingProgramsController(TrainingService training) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await training.ListCatalogueAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await training.GetCatalogueItemAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTrainingProgramRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.CreateAsync(request, cancellationToken));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTrainingProgramRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await training.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/granted-skills")]
    public async Task<IActionResult> SetGrantedSkills(
        int id,
        [FromBody] SetGrantedSkillsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.SetGrantedSkillsAsync(id, request, replaceAll: true, cancellationToken));

    [HttpPatch("{id:int}/granted-skills")]
    public async Task<IActionResult> PatchGrantedSkills(
        int id,
        [FromBody] SetGrantedSkillsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.SetGrantedSkillsAsync(id, request, replaceAll: false, cancellationToken));
}
