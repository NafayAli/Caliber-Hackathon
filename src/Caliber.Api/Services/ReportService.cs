using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Dashboard;
using Caliber.Api.Dtos.Reports;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class ReportService(
    ReadinessService readiness,
    CaliberDbContext db,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<ReadinessSummaryReportDto> GetReadinessSummaryAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var dashboard = await readiness.GetDashboardAsync(currentUser, cancellationToken);
        var employees = await GetScopedEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return new ReadinessSummaryReportDto
            {
                OverallCompliancePercent = 100m,
            };
        }

        var employeeIds = employees.Select(e => e.Id).ToArray();
        var requirementsByEmployee = await readiness.GetRequirementsForEmployeesAsync(employeeIds, cancellationToken);

        var gapStatuses = new HashSet<ReadinessStatus>
        {
            ReadinessStatus.Expired,
            ReadinessStatus.Overdue,
            ReadinessStatus.Missing,
            ReadinessStatus.InProgress,
        };

        var summaries = employees.Select(employee =>
        {
            if (!requirementsByEmployee.TryGetValue(employee.Id, out var requirements) || requirements.Count == 0)
            {
                return new EmployeeReadinessSummaryDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    LocationName = employee.LocationName,
                    ReadinessPercent = 100m,
                    WorstStatus = ReadinessStatus.Compliant,
                    GapCount = 0,
                };
            }

            var worst = requirements
                .Select(r => r.Status)
                .OrderBy(GetStatusPriority)
                .First();

            return new EmployeeReadinessSummaryDto
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                LocationName = employee.LocationName,
                ReadinessPercent = ReadinessService.ComputeReadinessPercent(requirements),
                WorstStatus = worst,
                GapCount = requirements.Count(r => gapStatuses.Contains(r.Status)),
            };
        }).OrderBy(e => e.EmployeeName).ToList();

        return new ReadinessSummaryReportDto
        {
            OverallCompliancePercent = dashboard.OverallCompliancePercent,
            TotalEmployees = employees.Count,
            EmployeesFullyReady = dashboard.EmployeesFullyReady,
            Employees = summaries,
        };
    }

    public async Task<ExpirationScheduleReportDto> GetExpirationScheduleAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var expirations = await readiness.GetExpirationsAsync(currentUser, cancellationToken);
        var total = expirations.Buckets.Sum(bucket => bucket.Items.Count);

        return new ExpirationScheduleReportDto
        {
            TotalExpiring = total,
            Buckets = expirations.Buckets,
        };
    }

    public async Task<ComplianceGapsReportDto> GetComplianceGapsAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var employees = await GetScopedEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return new ComplianceGapsReportDto();
        }

        var employeeLookup = employees.ToDictionary(e => e.Id);
        var employeeIds = employees.Select(e => e.Id).ToArray();
        var requirementsByEmployee = await readiness.GetRequirementsForEmployeesAsync(employeeIds, cancellationToken);

        var gapPriority = new Dictionary<ReadinessStatus, int>
        {
            [ReadinessStatus.Expired] = 0,
            [ReadinessStatus.Overdue] = 1,
            [ReadinessStatus.Missing] = 2,
            [ReadinessStatus.InProgress] = 3,
        };

        var gaps = requirementsByEmployee
            .SelectMany(pair => pair.Value.Select(req => (EmployeeId: pair.Key, Requirement: req)))
            .Where(item => gapPriority.ContainsKey(item.Requirement.Status))
            .OrderBy(item => gapPriority[item.Requirement.Status])
            .ThenBy(item => employeeLookup[item.EmployeeId].FullName)
            .ThenBy(item => item.Requirement.Name)
            .Select(item => new GapItemDto
            {
                EmployeeId = item.EmployeeId,
                EmployeeName = employeeLookup[item.EmployeeId].FullName,
                LocationName = employeeLookup[item.EmployeeId].LocationName,
                Kind = item.Requirement.Kind,
                RequirementName = item.Requirement.Name,
                Status = item.Requirement.Status,
            })
            .ToList();

        return new ComplianceGapsReportDto
        {
            TotalGaps = gaps.Count,
            Gaps = gaps,
        };
    }

    public async Task<SkillsMatrixReportDto> GetSkillsMatrixAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var employees = await GetScopedEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return new SkillsMatrixReportDto();
        }

        var skills = await db.Skills
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SkillColumnDto
            {
                SkillId = s.Id,
                SkillName = s.Name,
                Category = s.Category,
            })
            .ToListAsync(cancellationToken);

        if (skills.Count == 0)
        {
            return new SkillsMatrixReportDto { Skills = skills };
        }

        var employeeIds = employees.Select(e => e.Id).ToArray();
        var skillIds = skills.Select(s => s.SkillId).ToArray();

        var today = clock.Today;
        var assignments = await db.EmployeeSkills
            .AsNoTracking()
            .Where(es => employeeIds.Contains(es.EmployeeId)
                         && skillIds.Contains(es.SkillId)
                         && es.Status == EmployeeSkillStatus.Active
                         && (es.ExpiresOn == null || es.ExpiresOn >= today))
            .Select(es => new
            {
                es.EmployeeId,
                es.SkillId,
                es.ProficiencyLevel,
            })
            .ToListAsync(cancellationToken);

        var proficiencyLookup = assignments.ToDictionary(
            x => (x.EmployeeId, x.SkillId),
            x => x.ProficiencyLevel);

        var rows = employees.Select(employee => new SkillsMatrixRowDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            LocationName = employee.LocationName,
            Cells = skills.Select(skill => new SkillsMatrixCellDto
            {
                SkillId = skill.SkillId,
                ProficiencyLevel = proficiencyLookup.TryGetValue((employee.Id, skill.SkillId), out var level)
                    ? level
                    : null,
            }).ToList(),
        }).ToList();

        return new SkillsMatrixReportDto
        {
            Skills = skills,
            Rows = rows,
        };
    }

    public async Task<AtRiskEmployeesReportDto> GetAtRiskEmployeesAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var summaries = await BuildEmployeeSummariesAsync(cancellationToken);
        var atRisk = summaries
            .Select(s => ToAtRiskRow(s))
            .Where(row => row.RiskScore > 0)
            .OrderByDescending(row => row.RiskScore)
            .ThenBy(row => row.EmployeeName)
            .ToList();

        var critical = atRisk.Count(row => row.ExpiredCount > 0 || row.OverdueCount > 0);
        var avgReadiness = atRisk.Count == 0
            ? 100m
            : Math.Round(atRisk.Average(row => row.ReadinessPercent), 1);

        return new AtRiskEmployeesReportDto
        {
            TotalAtRisk = atRisk.Count,
            CriticalCount = critical,
            AvgReadinessPercent = avgReadiness,
            Employees = atRisk,
        };
    }

    public async Task<ComplianceLeadersReportDto> GetComplianceLeadersAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var employees = await GetScopedEmployeesAsync(cancellationToken);
        var summaries = await BuildEmployeeSummariesAsync(cancellationToken);
        var summaryLookup = summaries.ToDictionary(s => s.EmployeeId);
        var employeeIds = employees.Select(e => e.Id).ToArray();
        var requirementsByEmployee = await readiness.GetRequirementsForEmployeesAsync(employeeIds, cancellationToken);

        var leaders = employees
            .Select(employee =>
            {
                if (!summaryLookup.TryGetValue(employee.Id, out var summary))
                {
                    return null;
                }

                if (!requirementsByEmployee.TryGetValue(employee.Id, out var requirements))
                {
                    requirements = [];
                }

                if (!ReadinessService.IsEmployeeFullyReady(requirements))
                {
                    return null;
                }

                var tier = ClassifyLeaderTier(summary) ?? "Ready";

                return new ComplianceLeaderRowDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    LocationName = employee.LocationName,
                    Tier = tier,
                    ReadinessPercent = summary.ReadinessPercent,
                    WorstStatus = summary.WorstStatus,
                };
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderBy(row => row.Tier == "Gold" ? 0 : row.Tier == "Silver" ? 1 : 2)
            .ThenByDescending(row => row.ReadinessPercent)
            .ThenBy(row => row.EmployeeName)
            .ToList();

        var gold = leaders.Count(row => row.Tier == "Gold");
        var silver = leaders.Count(row => row.Tier == "Silver");
        var ready = leaders.Count(row => row.Tier == "Ready");
        var fullyReadyCount = leaders.Count;
        var workforceReady = employees.Count == 0
            ? 0m
            : Math.Round(fullyReadyCount * 100m / employees.Count, 1);

        return new ComplianceLeadersReportDto
        {
            FullyReadyCount = fullyReadyCount,
            GoldCount = gold,
            SilverCount = silver,
            ReadyCount = ready,
            WorkforceReadyPercent = workforceReady,
            Leaders = leaders,
        };
    }

    public async Task<LocationScorecardReportDto> GetLocationScorecardAsync(CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var employees = await GetScopedEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return new LocationScorecardReportDto();
        }

        var summaries = await BuildEmployeeSummariesAsync(cancellationToken);
        var summaryLookup = summaries.ToDictionary(s => s.EmployeeId);

        var gapStatuses = new HashSet<ReadinessStatus>
        {
            ReadinessStatus.Expired,
            ReadinessStatus.Overdue,
            ReadinessStatus.Missing,
            ReadinessStatus.InProgress,
        };

        var locationRows = employees
            .GroupBy(e => new { e.LocationId, e.LocationName })
            .Select(group =>
            {
                var groupSummaries = group
                    .Select(e => summaryLookup[e.Id])
                    .ToList();

                var fullyReady = groupSummaries.Count(s =>
                    s.GapCount == 0
                    && s.WorstStatus is ReadinessStatus.Compliant or ReadinessStatus.Waived);

                var atRisk = groupSummaries.Count(s =>
                    s.WorstStatus is ReadinessStatus.Expired or ReadinessStatus.Overdue
                    or ReadinessStatus.Missing);

                var expiringSoon = groupSummaries.Count(s =>
                    s.WorstStatus == ReadinessStatus.ExpiringSoon);

                var compliance = groupSummaries.Count == 0
                    ? 100m
                    : Math.Round(groupSummaries.Average(s => s.ReadinessPercent), 1);

                var fullyReadyPercent = group.Count() == 0
                    ? 0m
                    : Math.Round(fullyReady * 100m / group.Count(), 1);

                return new LocationScorecardRowDto
                {
                    LocationId = group.Key.LocationId,
                    LocationName = group.Key.LocationName,
                    EmployeeCount = group.Count(),
                    FullyReadyCount = fullyReady,
                    AtRiskCount = atRisk,
                    ExpiringSoonCount = expiringSoon,
                    CompliancePercent = compliance,
                    FullyReadyPercent = fullyReadyPercent,
                };
            })
            .OrderByDescending(row => row.CompliancePercent)
            .ThenBy(row => row.LocationName)
            .ToList();

        for (var index = 0; index < locationRows.Count; index++)
        {
            locationRows[index] = locationRows[index] with { Rank = index + 1 };
        }

        var orgCompliance = Math.Round(summaries.Average(s => s.ReadinessPercent), 1);

        return new LocationScorecardReportDto
        {
            OrgCompliancePercent = orgCompliance,
            TopLocationName = locationRows.FirstOrDefault()?.LocationName,
            BottomLocationName = locationRows.LastOrDefault()?.LocationName,
            Locations = locationRows,
        };
    }

    private async Task<IReadOnlyList<EmployeeSummary>> BuildEmployeeSummariesAsync(CancellationToken cancellationToken)
    {
        var employees = await GetScopedEmployeesAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return [];
        }

        var employeeIds = employees.Select(e => e.Id).ToArray();
        var requirementsByEmployee = await readiness.GetRequirementsForEmployeesAsync(employeeIds, cancellationToken);

        var gapStatuses = new HashSet<ReadinessStatus>
        {
            ReadinessStatus.Expired,
            ReadinessStatus.Overdue,
            ReadinessStatus.Missing,
            ReadinessStatus.InProgress,
        };

        var gapPriority = new Dictionary<ReadinessStatus, int>
        {
            [ReadinessStatus.Expired] = 0,
            [ReadinessStatus.Overdue] = 1,
            [ReadinessStatus.Missing] = 2,
            [ReadinessStatus.InProgress] = 3,
        };

        return employees.Select(employee =>
        {
            if (!requirementsByEmployee.TryGetValue(employee.Id, out var requirements) || requirements.Count == 0)
            {
                return new EmployeeSummary(
                    employee.Id,
                    employee.FullName,
                    employee.LocationId,
                    employee.LocationName,
                    100m,
                    ReadinessStatus.Compliant,
                    0,
                    0,
                    0,
                    0,
                    0,
                    string.Empty);
            }

            var worst = requirements
                .Select(r => r.Status)
                .OrderBy(GetStatusPriority)
                .First();

            var topGap = requirements
                .Where(r => gapPriority.ContainsKey(r.Status))
                .OrderBy(r => gapPriority[r.Status])
                .ThenBy(r => r.Name)
                .Select(r => r.Name)
                .FirstOrDefault() ?? string.Empty;

            return new EmployeeSummary(
                employee.Id,
                employee.FullName,
                employee.LocationId,
                employee.LocationName,
                ReadinessService.ComputeReadinessPercent(requirements),
                worst,
                requirements.Count(r => r.Status == ReadinessStatus.Expired),
                requirements.Count(r => r.Status == ReadinessStatus.Overdue),
                requirements.Count(r => r.Status == ReadinessStatus.Missing),
                requirements.Count(r => r.Status == ReadinessStatus.InProgress),
                requirements.Count(r => gapStatuses.Contains(r.Status)),
                topGap);
        }).ToList();
    }

    private static AtRiskEmployeeRowDto ToAtRiskRow(EmployeeSummary summary)
    {
        var riskScore = (summary.ExpiredCount * 100)
                        + (summary.OverdueCount * 50)
                        + (summary.MissingCount * 20)
                        + (summary.InProgressCount * 5)
                        + (int)Math.Round(100m - summary.ReadinessPercent);

        return new AtRiskEmployeeRowDto
        {
            EmployeeId = summary.EmployeeId,
            EmployeeName = summary.EmployeeName,
            LocationName = summary.LocationName,
            ReadinessPercent = summary.ReadinessPercent,
            ExpiredCount = summary.ExpiredCount,
            OverdueCount = summary.OverdueCount,
            MissingCount = summary.MissingCount,
            WorstStatus = summary.WorstStatus,
            TopGapName = summary.TopGapName,
            RiskScore = riskScore,
        };
    }

    private static string? ClassifyLeaderTier(EmployeeSummary summary)
    {
        if (summary.ReadinessPercent >= 100m
            && summary.GapCount == 0
            && summary.WorstStatus is ReadinessStatus.Compliant or ReadinessStatus.Waived)
        {
            return "Gold";
        }

        if (summary.ReadinessPercent >= 95m
            && summary.ExpiredCount == 0
            && summary.OverdueCount == 0
            && summary.MissingCount == 0)
        {
            return "Silver";
        }

        return null;
    }

    private async Task<IReadOnlyList<ScopedEmployee>> GetScopedEmployeesAsync(CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking().Where(e => e.IsActive);

        query = currentUser.AccessLevel switch
        {
            AccessLevel.Admin => query,
            AccessLevel.Manager => query.Where(e => e.LocationId == currentUser.LocationId),
            _ => query.Where(_ => false),
        };

        return await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new ScopedEmployee(
                e.Id,
                e.FirstName + " " + e.LastName,
                e.LocationId,
                e.Location.Name))
            .ToListAsync(cancellationToken);
    }

    private void EnsureManagerOrAdmin()
    {
        if (currentUser.AccessLevel == AccessLevel.Technician)
        {
            throw new ForbiddenException("Only managers and administrators may access reports.");
        }
    }

    private static int GetStatusPriority(ReadinessStatus status) =>
        status switch
        {
            ReadinessStatus.Expired => 0,
            ReadinessStatus.Overdue => 1,
            ReadinessStatus.Missing => 2,
            ReadinessStatus.InProgress => 3,
            ReadinessStatus.ExpiringSoon => 4,
            ReadinessStatus.Compliant => 5,
            ReadinessStatus.Waived => 6,
            _ => 7,
        };

    private sealed record ScopedEmployee(int Id, string FullName, int LocationId, string LocationName);

    private sealed record EmployeeSummary(
        int EmployeeId,
        string EmployeeName,
        int LocationId,
        string LocationName,
        decimal ReadinessPercent,
        ReadinessStatus WorstStatus,
        int ExpiredCount,
        int OverdueCount,
        int MissingCount,
        int InProgressCount,
        int GapCount,
        string TopGapName);
}
