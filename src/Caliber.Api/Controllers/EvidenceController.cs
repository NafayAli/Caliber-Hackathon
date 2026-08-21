using Caliber.Api.Dtos.Evidence;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/evidence")]
public sealed class EvidenceController(EvidenceService evidence) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    [EnableRateLimiting("uploads")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EvidenceDto>> Upload(
        [FromForm] EvidenceUploadRequest request,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "An evidence file is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await evidence.UploadAsync(
            request,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return CreatedAtAction(nameof(ViewContent), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}/content")]
    public async Task<IActionResult> ViewContent(int id, CancellationToken cancellationToken)
    {
        var (content, metadata) = await evidence.OpenContentAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{metadata.FileName}\"";
        return File(content, metadata.ContentType);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> DownloadContent(int id, CancellationToken cancellationToken)
    {
        var (content, metadata) = await evidence.OpenContentAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(content, metadata.ContentType, metadata.FileName);
    }

    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<EvidenceDto>> Verify(
        int id,
        [FromBody] VerifyEvidenceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await evidence.VerifyAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await evidence.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
