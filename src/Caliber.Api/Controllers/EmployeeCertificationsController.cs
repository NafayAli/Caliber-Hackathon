using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/employee-certifications")]
public sealed class EmployeeCertificationsController(CertificationService certifications) : ControllerBase
{
    [HttpPost("{id:int}/awards")]
    public async Task<IActionResult> RecordAward(
        int id,
        [FromBody] RecordAwardRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.RecordAwardAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/waive")]
    public async Task<IActionResult> Waive(
        int id,
        [FromBody] WaiveAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.WaiveAsync(id, request, cancellationToken));
}
