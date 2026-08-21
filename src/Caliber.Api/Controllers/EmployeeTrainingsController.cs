using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/employee-trainings")]
public sealed class EmployeeTrainingsController(TrainingService training) : ControllerBase
{
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateProgress(
        int id,
        [FromBody] UpdateTrainingProgressRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.UpdateProgressAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody] CompleteTrainingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.CompleteAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(
        int id,
        [FromBody] AcknowledgeTrainingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.AcknowledgeAsync(id, request, cancellationToken));
}
