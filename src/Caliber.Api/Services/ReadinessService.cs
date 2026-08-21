using Caliber.Api.Abstractions;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Common;
using Caliber.Api.Dtos.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

/// <summary>
/// Single source of truth for computed readiness status. No controller, query, or UI
/// component may derive status independently.
/// </summary>
public sealed class ReadinessService(CaliberDbContext db, IClock clock)
{
    /// <summary>
    /// Ordered evaluation rules from solution.md. Order is intentional.
    /// </summary>
    public static ReadinessStatus ComputeStatus(
        AssignmentStatus assignmentStatus,
        bool isCompleted,
        DateOnly? effectiveDate,
        DateOnly? dueOn,
        int warningDays,
        DateOnly today)
    {
        if (assignmentStatus == AssignmentStatus.Waived)
        {
            return ReadinessStatus.Waived;
        }

        if (isCompleted && effectiveDate is not null && effectiveDate < today)
        {
            return ReadinessStatus.Expired;
        }

        if (isCompleted && effectiveDate is not null && effectiveDate <= today.AddDays(warningDays))
        {
            return ReadinessStatus.ExpiringSoon;
        }

        if (isCompleted)
        {
            return ReadinessStatus.Compliant;
        }

        if (!isCompleted && dueOn is not null && dueOn < today)
        {
            return ReadinessStatus.Overdue;
        }

        if (assignmentStatus == AssignmentStatus.InProgress)
        {
            return ReadinessStatus.InProgress;
        }

        return ReadinessStatus.Missing;
    }

    public static decimal ComputeReadinessPercent(IEnumerable<RequirementStatusDto> requirements)
    {
        var mandatory = requirements.Where(r => r.IsMandatory).ToList();
        if (mandatory.Count == 0)
        {
            return 100m;
        }

        var compliant = mandatory.Count(r =>
            r.Status == ReadinessStatus.Compliant || r.Status == ReadinessStatus.ExpiringSoon);

        return Math.Round(100m * compliant / mandatory.Count, 1);
    }

    /// <summary>
    /// Matches dashboard "fully ready" — every mandatory requirement is compliant, expiring soon, or waived.
    /// </summary>
    public static bool IsEmployeeFullyReady(IEnumerable<RequirementStatusDto> requirements)
    {
        var mandatory = requirements.Where(r => r.IsMandatory).ToList();
        if (mandatory.Count == 0)
        {
            return true;
        }

        return mandatory.All(r =>
            r.Status is ReadinessStatus.Compliant
                or ReadinessStatus.ExpiringSoon
                or ReadinessStatus.Waived);
    }

    public async Task<IReadOnlyList<RequirementStatusDto>> GetRequirementsForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetRequirementRowsAsync([employeeId], cancellationToken);
        return ApplyStatus(rows, clock.Today);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<RequirementStatusDto>>> GetRequirementsForEmployeesAsync(
        IEnumerable<int> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, IReadOnlyList<RequirementStatusDto>>();
        }

        var rows = await GetRequirementRowsAsync(ids, cancellationToken);
        var today = clock.Today;

        return rows
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RequirementStatusDto>)ApplyStatus(g, today));
    }

    public async Task<DashboardDto> GetDashboardAsync(ICurrentUser user, CancellationToken cancellationToken = default)
    {
        var employees = await GetScopedEmployeesAsync(user, cancellationToken);
        if (employees.Count == 0)
        {
            return new DashboardDto();
        }

        var employeeLookup = employees.ToDictionary(e => e.Id);
        var employeeIds = employees.Select(e => e.Id).ToArray();
        var today = clock.Today;

        var rows = await GetRequirementRowsAsync(employeeIds, cancellationToken);
        var requirementsByEmployee = rows
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => ApplyStatus(g, today));

        var allRequirements = requirementsByEmployee
            .SelectMany(pair => pair.Value.Select(req => (EmployeeId: pair.Key, Requirement: req)))
            .ToList();

        var employeePercents = employees.Select(employee =>
        {
            if (!requirementsByEmployee.TryGetValue(employee.Id, out var reqs) || reqs.Count == 0)
            {
                return 100m;
            }

            return ComputeReadinessPercent(reqs);
        }).ToList();

        var overallCompliance = employeePercents.Count == 0
            ? 100m
            : Math.Round(employeePercents.Average(), 1);

        var fullyReady = employees.Count(employee =>
        {
            if (!requirementsByEmployee.TryGetValue(employee.Id, out var reqs) || reqs.Count == 0)
            {
                return true;
            }

            return IsEmployeeFullyReady(reqs);
        });

        var expiringWithin60 = allRequirements.Count(item =>
            item.Requirement.EffectiveDate is DateOnly effective
            && effective >= today
            && effective <= today.AddDays(60));

        var expiredOrOverdue = allRequirements.Count(item =>
            item.Requirement.Status is ReadinessStatus.Expired or ReadinessStatus.Overdue);

        var expiringFeed = allRequirements
            .Where(item => item.Requirement.Status == ReadinessStatus.ExpiringSoon
                           && item.Requirement.EffectiveDate is not null)
            .Select(item => ToExpiringItem(item.EmployeeId, item.Requirement, employeeLookup))
            .OrderBy(item => item.EffectiveDate)
            .Take(10)
            .ToList();

        var byLocation = employees
            .GroupBy(e => new { e.LocationId, e.LocationName })
            .Select(group =>
            {
                var locationPercents = group.Select(employee =>
                {
                    if (!requirementsByEmployee.TryGetValue(employee.Id, out var reqs) || reqs.Count == 0)
                    {
                        return 100m;
                    }

                    return ComputeReadinessPercent(reqs);
                }).ToList();

                return new LocationComplianceDto
                {
                    LocationId = group.Key.LocationId,
                    LocationName = group.Key.LocationName,
                    CompliancePercent = locationPercents.Count == 0
                        ? 100m
                        : Math.Round(locationPercents.Average(), 1),
                    EmployeeCount = group.Count(),
                };
            })
            .OrderBy(location => location.LocationName)
            .ToList();

        var gapPriority = new Dictionary<ReadinessStatus, int>
        {
            [ReadinessStatus.Expired] = 0,
            [ReadinessStatus.Overdue] = 1,
            [ReadinessStatus.Missing] = 2,
            [ReadinessStatus.InProgress] = 3,
        };

        var topGaps = allRequirements
            .Where(item => gapPriority.ContainsKey(item.Requirement.Status))
            .OrderBy(item => gapPriority[item.Requirement.Status])
            .ThenBy(item => employeeLookup[item.EmployeeId].FullName)
            .Take(10)
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

        var statusBreakdown = allRequirements
            .GroupBy(item => item.Requirement.Status)
            .Select(group => new StatusBreakdownDto
            {
                Status = group.Key,
                Count = group.Count(),
            })
            .OrderBy(item => item.Status)
            .ToList();

        var openGapStatuses = gapPriority.Keys.ToHashSet();
        var openGapsByKind = allRequirements
            .Where(item => openGapStatuses.Contains(item.Requirement.Status))
            .GroupBy(item => item.Requirement.Kind)
            .Select(group => new KindBreakdownDto
            {
                Kind = group.Key,
                Count = group.Count(),
            })
            .OrderBy(item => item.Kind)
            .ToList();

        var upcomingRenewals = allRequirements
            .Where(item => item.Requirement.EffectiveDate is DateOnly effective
                           && effective >= today
                           && effective <= today.AddDays(90)
                           && item.Requirement.Status != ReadinessStatus.Expired)
            .Select(item => item.Requirement.EffectiveDate!.Value)
            .ToList();

        var renewalHorizon = new[]
        {
            (Label: "Next 30 days", MinDays: 0, MaxDays: 30),
            (Label: "31–60 days", MinDays: 31, MaxDays: 60),
            (Label: "61–90 days", MinDays: 61, MaxDays: 90),
        }.Select(bucket => new RenewalHorizonDto
        {
            Label = bucket.Label,
            Count = upcomingRenewals.Count(effective =>
            {
                var daysUntil = effective.DayNumber - today.DayNumber;
                return daysUntil >= bucket.MinDays && daysUntil <= bucket.MaxDays;
            }),
        }).ToList();

        var fullyReadyPercent = employees.Count == 0
            ? 100m
            : Math.Round(fullyReady * 100m / employees.Count, 1);

        return new DashboardDto
        {
            TotalEmployees = employees.Count,
            OverallCompliancePercent = overallCompliance,
            EmployeesFullyReady = fullyReady,
            FullyReadyPercent = fullyReadyPercent,
            ExpiringWithin60Days = expiringWithin60,
            ExpiredOrOverdue = expiredOrOverdue,
            ExpiringSoonFeed = expiringFeed,
            ByLocation = byLocation,
            TopGaps = topGaps,
            StatusBreakdown = statusBreakdown,
            OpenGapsByKind = openGapsByKind,
            RenewalHorizon = renewalHorizon,
        };
    }

    public async Task<ExpirationsDto> GetExpirationsAsync(ICurrentUser user, CancellationToken cancellationToken = default)
    {
        var employees = await GetScopedEmployeesAsync(user, cancellationToken);
        if (employees.Count == 0)
        {
            return new ExpirationsDto();
        }

        var employeeLookup = employees.ToDictionary(e => e.Id);
        var employeeIds = employees.Select(e => e.Id).ToArray();
        var today = clock.Today;

        var rows = await GetRequirementRowsAsync(employeeIds, cancellationToken);
        var requirements = rows
            .GroupBy(r => r.EmployeeId)
            .SelectMany(g => ApplyStatus(g, today).Select(req => (EmployeeId: g.Key, Requirement: req)))
            .Where(item => item.Requirement.EffectiveDate is DateOnly effective
                           && effective >= today
                           && effective <= today.AddDays(90)
                           && item.Requirement.Status != ReadinessStatus.Expired)
            .Select(item => ToExpiringItem(item.EmployeeId, item.Requirement, employeeLookup))
            .OrderBy(item => item.EffectiveDate)
            .ToList();

        var buckets = new[]
        {
            (Days: 30, Label: "Next 30 days", MinDays: 0, MaxDays: 30),
            (Days: 60, Label: "31–60 days", MinDays: 31, MaxDays: 60),
            (Days: 90, Label: "61–90 days", MinDays: 61, MaxDays: 90),
        };

        var bucketDtos = buckets.Select(bucket => new ExpirationBucketDto
        {
            Days = bucket.Days,
            Label = bucket.Label,
            Items = requirements
                .Where(item =>
                {
                    var daysUntil = item.EffectiveDate.DayNumber - today.DayNumber;
                    return daysUntil >= bucket.MinDays && daysUntil <= bucket.MaxDays;
                })
                .ToList(),
        }).ToList();

        return new ExpirationsDto { Buckets = bucketDtos };
    }

    private static ExpiringItemDto ToExpiringItem(
        int employeeId,
        RequirementStatusDto requirement,
        IReadOnlyDictionary<int, ScopedEmployee> employees) =>
        new()
        {
            EmployeeId = employeeId,
            EmployeeName = employees[employeeId].FullName,
            LocationName = employees[employeeId].LocationName,
            Kind = requirement.Kind,
            RequirementName = requirement.Name,
            EffectiveDate = requirement.EffectiveDate!.Value,
            Status = requirement.Status,
        };

    private async Task<IReadOnlyList<ScopedEmployee>> GetScopedEmployeesAsync(
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking().Where(e => e.IsActive);

        query = user.AccessLevel switch
        {
            AccessLevel.Admin => query,
            AccessLevel.Manager => query.Where(e => e.LocationId == user.LocationId),
            AccessLevel.Technician => query.Where(e => e.Id == user.EmployeeId),
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

    private async Task<List<RequirementRow>> GetRequirementRowsAsync(
        int[] employeeIds,
        CancellationToken cancellationToken)
    {
        var latestAwards = db.CertificationAwards
            .AsNoTracking()
            .GroupBy(a => a.EmployeeCertificationId)
            .Select(g => new
            {
                EmployeeCertificationId = g.Key,
                AwardedOn = g.Max(a => a.AwardedOn),
            });

        var certRows = await (
            from ec in db.EmployeeCertifications.AsNoTracking()
            join c in db.Certifications.AsNoTracking() on ec.CertificationId equals c.Id
            join la in latestAwards on ec.Id equals la.EmployeeCertificationId into awards
            from la in awards.DefaultIfEmpty()
            join award in db.CertificationAwards.AsNoTracking()
                on new { ec.Id, la.AwardedOn } equals new { Id = award.EmployeeCertificationId, award.AwardedOn } into awardRows
            from award in awardRows.DefaultIfEmpty()
            where employeeIds.Contains(ec.EmployeeId)
            select new RequirementRow
            {
                EmployeeId = ec.EmployeeId,
                Kind = RequirementKind.Certification,
                SourceId = ec.Id,
                CatalogueId = c.Id,
                Name = c.Name,
                Category = c.Category.ToString(),
                AssignmentStatus = ec.Status,
                CompletedOn = award != null ? award.AwardedOn : null,
                EffectiveDate = award != null ? award.ExpiresOn : null,
                DueOn = ec.DueOn,
                WarningDays = c.ExpiryWarningDays,
                IsMandatory = true,
                RowVersion = ec.RowVersion == null ? Array.Empty<byte>() : ec.RowVersion,
            }).ToListAsync(cancellationToken);

        var trainingRows = await (
            from et in db.EmployeeTrainings.AsNoTracking()
            join t in db.TrainingPrograms.AsNoTracking() on et.TrainingProgramId equals t.Id
            where employeeIds.Contains(et.EmployeeId)
            select new RequirementRow
            {
                EmployeeId = et.EmployeeId,
                Kind = RequirementKind.Training,
                SourceId = et.Id,
                CatalogueId = t.Id,
                Name = t.Name,
                Category = t.Category.ToString(),
                AssignmentStatus = et.Status,
                CompletedOn = et.CompletedOn,
                EffectiveDate = et.NextDueOn,
                DueOn = et.DueOn,
                WarningDays = t.ExpiryWarningDays,
                IsMandatory = true,
                RowVersion = et.RowVersion == null ? Array.Empty<byte>() : et.RowVersion,
                RequiresAcknowledgement = t.RequiresAcknowledgement,
                AcknowledgedOn = et.AcknowledgedOn,
            }).ToListAsync(cancellationToken);

        certRows.AddRange(trainingRows);

        var pendingRenewals = await db.RenewalRequests
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.Status == RenewalRequestStatus.Pending)
            .Select(r => new { r.EmployeeId, r.Kind, r.AssignmentId, r.Id })
            .ToListAsync(cancellationToken);

        var renewalLookup = pendingRenewals.ToDictionary(
            r => (r.EmployeeId, r.Kind, r.AssignmentId),
            r => r.Id);

        foreach (var row in certRows)
        {
            if (renewalLookup.TryGetValue((row.EmployeeId, row.Kind, row.SourceId), out var renewalId))
            {
                row.PendingRenewalRequestId = renewalId;
            }
        }

        return certRows;
    }

    private static IReadOnlyList<RequirementStatusDto> ApplyStatus(IEnumerable<RequirementRow> rows, DateOnly today) =>
        rows.Select(row =>
        {
            var isCompleted = row.AssignmentStatus == AssignmentStatus.Completed
                              || row.AssignmentStatus == AssignmentStatus.Waived
                              || row.CompletedOn is not null;

            return new RequirementStatusDto
            {
                Kind = row.Kind,
                SourceId = row.SourceId,
                CatalogueId = row.CatalogueId,
                Name = row.Name,
                Category = row.Category,
                AssignmentStatus = row.AssignmentStatus,
                CompletedOn = row.CompletedOn,
                EffectiveDate = row.EffectiveDate,
                DueOn = row.DueOn,
                WarningDays = row.WarningDays,
                IsMandatory = row.IsMandatory,
                Status = ComputeStatus(
                    row.AssignmentStatus,
                    isCompleted,
                    row.EffectiveDate,
                    row.DueOn,
                    row.WarningDays,
                    today),
                RowVersion = row.RowVersion,
                RequiresAcknowledgement = row.RequiresAcknowledgement,
                AcknowledgedOn = row.AcknowledgedOn,
                PendingRenewalRequestId = row.PendingRenewalRequestId,
            };
        }).ToList();

    private sealed class RequirementRow
    {
        public int EmployeeId { get; init; }

        public RequirementKind Kind { get; init; }

        public int SourceId { get; init; }

        public int CatalogueId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public AssignmentStatus AssignmentStatus { get; init; }

        public DateOnly? CompletedOn { get; init; }

        public DateOnly? EffectiveDate { get; init; }

        public DateOnly? DueOn { get; init; }

        public int WarningDays { get; init; }

        public bool IsMandatory { get; init; } = true;

        public byte[] RowVersion { get; init; } = [];

        public bool RequiresAcknowledgement { get; set; }

        public DateTimeOffset? AcknowledgedOn { get; init; }

        public int? PendingRenewalRequestId { get; set; }
    }

    private sealed record ScopedEmployee(
        int Id,
        string FullName,
        int LocationId,
        string LocationName);
}
