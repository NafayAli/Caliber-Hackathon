using Caliber.Api.Dtos.Notifications;
using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        Ok(await notifications.GetSummaryAsync(cancellationToken: cancellationToken));

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        await notifications.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(
        [FromBody] BroadcastAnnouncementRequest request,
        CancellationToken cancellationToken) =>
        Ok(new { sent = await notifications.BroadcastAnnouncementAsync(request, cancellationToken) });

    [HttpPost("notify-employees")]
    public async Task<IActionResult> NotifyEmployees(
        [FromBody] NotifyEmployeesRequest request,
        CancellationToken cancellationToken) =>
        Ok(new { sent = await notifications.NotifyEmployeesAsync(request, cancellationToken) });
}

[ApiController]
[Route("api/renewal-requests")]
public sealed class RenewalRequestsController(RenewalService renewals) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> ListPending(CancellationToken cancellationToken) =>
        Ok(await renewals.ListPendingAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRenewalRequestBody request,
        CancellationToken cancellationToken) =>
        Ok(await renewals.RequestRenewalAsync(request, cancellationToken));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] ReviewRenewalRequestBody request,
        CancellationToken cancellationToken) =>
        Ok(await renewals.ApproveAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(
        int id,
        [FromBody] ReviewRenewalRequestBody request,
        CancellationToken cancellationToken) =>
        Ok(await renewals.DeclineAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/renewals")]
public sealed class RenewalsController(RenewalService renewals) : ControllerBase
{
    [HttpPost("direct")]
    public async Task<IActionResult> DirectRenew(
        [FromBody] DirectRenewRequestBody request,
        CancellationToken cancellationToken)
    {
        await renewals.DirectRenewAsync(request, cancellationToken);
        return NoContent();
    }
}
