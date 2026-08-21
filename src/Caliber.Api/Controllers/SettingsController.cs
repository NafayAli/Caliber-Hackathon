using Caliber.Api.Dtos.Settings;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(SettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await settings.GetSettingsAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateAppSettingsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await settings.UpdateSettingsAsync(request, cancellationToken));

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken cancellationToken) =>
        Ok(await settings.GetModuleAccessAsync(cancellationToken));

    [HttpGet("modules/me")]
    public async Task<IActionResult> GetMyModules(CancellationToken cancellationToken) =>
        Ok(await settings.GetModuleAccessForCurrentUserAsync(cancellationToken));

    [HttpPut("modules")]
    public async Task<IActionResult> UpdateModule(
        [FromBody] UpdateModuleAccessRequest request,
        CancellationToken cancellationToken)
    {
        await settings.UpdateModuleAccessAsync(request, cancellationToken);
        return Ok(await settings.GetModuleAccessAsync(cancellationToken));
    }
}
