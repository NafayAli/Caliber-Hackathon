using Caliber.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("readiness-summary")]
    public async Task<IActionResult> ReadinessSummary(CancellationToken cancellationToken) =>
        Ok(await reports.GetReadinessSummaryAsync(cancellationToken));

    [HttpGet("expiration-schedule")]
    public async Task<IActionResult> ExpirationSchedule(CancellationToken cancellationToken) =>
        Ok(await reports.GetExpirationScheduleAsync(cancellationToken));

    [HttpGet("compliance-gaps")]
    public async Task<IActionResult> ComplianceGaps(CancellationToken cancellationToken) =>
        Ok(await reports.GetComplianceGapsAsync(cancellationToken));

    [HttpGet("skills-matrix")]
    public async Task<IActionResult> SkillsMatrix(CancellationToken cancellationToken) =>
        Ok(await reports.GetSkillsMatrixAsync(cancellationToken));

    [HttpGet("at-risk-employees")]
    public async Task<IActionResult> AtRiskEmployees(CancellationToken cancellationToken) =>
        Ok(await reports.GetAtRiskEmployeesAsync(cancellationToken));

    [HttpGet("compliance-leaders")]
    public async Task<IActionResult> ComplianceLeaders(CancellationToken cancellationToken) =>
        Ok(await reports.GetComplianceLeadersAsync(cancellationToken));

    [HttpGet("location-scorecard")]
    public async Task<IActionResult> LocationScorecard(CancellationToken cancellationToken) =>
        Ok(await reports.GetLocationScorecardAsync(cancellationToken));
}
