using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/certifications")]
public sealed class CertificationsController(CertificationService certifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await certifications.ListCatalogueAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await certifications.GetCatalogueItemAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCertificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.CreateAsync(request, cancellationToken));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCertificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await certifications.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/granted-skills")]
    public async Task<IActionResult> SetGrantedSkills(
        int id,
        [FromBody] SetGrantedSkillsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.SetGrantedSkillsAsync(id, request, replaceAll: true, cancellationToken));

    [HttpPatch("{id:int}/granted-skills")]
    public async Task<IActionResult> PatchGrantedSkills(
        int id,
        [FromBody] SetGrantedSkillsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.SetGrantedSkillsAsync(id, request, replaceAll: false, cancellationToken));
}
