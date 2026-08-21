using Caliber.Api.Dtos.Settings;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/skill-requests")]
public sealed class SkillRequestsController(SkillAssignmentRequestService skillRequests) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> ListPending(CancellationToken cancellationToken) =>
        Ok(await skillRequests.ListPendingAsync(cancellationToken));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] ReviewSkillAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await skillRequests.ApproveAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] ReviewSkillAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await skillRequests.RejectAsync(id, request, cancellationToken));
}
