using Caliber.Api.Dtos.Employees;
using Caliber.Api.Dtos.Requests;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(
    EmployeeService employees,
    CertificationService certifications,
    TrainingService training,
    SkillService skills,
    SkillAssignmentRequestService skillRequests) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] EmployeeListQuery query, CancellationToken cancellationToken) =>
        Ok(await employees.ListAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await employees.GetProfileAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await employees.CreateAsync(request, cancellationToken));

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await employees.UpdateAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/requirements")]
    public async Task<IActionResult> Requirements(int id, CancellationToken cancellationToken) =>
        Ok(await employees.GetRequirementsAsync(id, cancellationToken));

    [HttpPost("{id:int}/certifications")]
    public async Task<IActionResult> AssignCertification(
        int id,
        [FromBody] AssignCertificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await certifications.AssignAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/training")]
    public async Task<IActionResult> AssignTraining(
        int id,
        [FromBody] AssignTrainingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await training.AssignAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/skills")]
    public async Task<IActionResult> AssignSkill(
        int id,
        [FromBody] AssignSkillRequest request,
        CancellationToken cancellationToken) =>
        Ok(await skills.AssignOrAssessAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/skill-requests")]
    public async Task<IActionResult> RequestSkill(
        int id,
        [FromBody] Dtos.Settings.CreateSkillAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await skillRequests.CreateAsync(id, request, cancellationToken));
}
