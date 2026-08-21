using Caliber.Api.Abstractions;
using Caliber.Api.Dtos.Auth;
using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(EmployeeService employees, ICurrentUser user) : ControllerBase
{
    [HttpGet("requirements")]
    public async Task<IActionResult> Requirements(CancellationToken cancellationToken) =>
        Ok(await employees.GetRequirementsAsync(user.EmployeeId, cancellationToken));

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await employees.UpdateProfileAsync(request, cancellationToken));

    [HttpPost("avatar")]
    [RequestSizeLimit(2_097_152)]
    [EnableRateLimiting("uploads")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AuthUserDto>> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "An avatar image is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await employees.UploadAvatarAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(result);
    }

    [HttpGet("avatar")]
    public async Task<IActionResult> GetOwnAvatar(CancellationToken cancellationToken)
    {
        var (content, contentType) = await employees.OpenAvatarAsync(user.EmployeeId, cancellationToken);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, max-age=3600";
        return File(content, contentType);
    }

    [HttpGet("avatar/{employeeId:int}")]
    public async Task<IActionResult> GetAvatar(int employeeId, CancellationToken cancellationToken)
    {
        var (content, contentType) = await employees.OpenAvatarAsync(employeeId, cancellationToken);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, max-age=3600";
        return File(content, contentType);
    }
}

[ApiController]
[Route("api/personas")]
public sealed class PersonasController(EmployeeService employees) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await employees.ListPersonasAsync(cancellationToken));
}
