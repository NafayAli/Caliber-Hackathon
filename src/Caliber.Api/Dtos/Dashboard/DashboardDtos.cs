using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Dashboard;

public sealed record DashboardDto
{
    public int TotalEmployees { get; init; }

    public decimal OverallCompliancePercent { get; init; }

    public int EmployeesFullyReady { get; init; }

    public decimal FullyReadyPercent { get; init; }

    public int ExpiringWithin60Days { get; init; }

    public int ExpiredOrOverdue { get; init; }

    public IReadOnlyList<ExpiringItemDto> ExpiringSoonFeed { get; init; } = [];

    public IReadOnlyList<LocationComplianceDto> ByLocation { get; init; } = [];

    public IReadOnlyList<GapItemDto> TopGaps { get; init; } = [];

    public IReadOnlyList<StatusBreakdownDto> StatusBreakdown { get; init; } = [];

    public IReadOnlyList<KindBreakdownDto> OpenGapsByKind { get; init; } = [];

    public IReadOnlyList<RenewalHorizonDto> RenewalHorizon { get; init; } = [];
}

public sealed record StatusBreakdownDto
{
    public ReadinessStatus Status { get; init; }

    public int Count { get; init; }
}

public sealed record KindBreakdownDto
{
    public RequirementKind Kind { get; init; }

    public int Count { get; init; }
}

public sealed record RenewalHorizonDto
{
    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed record ExpiringItemDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public RequirementKind Kind { get; init; }

    public string RequirementName { get; init; } = string.Empty;

    public DateOnly EffectiveDate { get; init; }

    public ReadinessStatus Status { get; init; }
}

public sealed record LocationComplianceDto
{
    public int LocationId { get; init; }

    public string LocationName { get; init; } = string.Empty;

    public decimal CompliancePercent { get; init; }

    public int EmployeeCount { get; init; }
}

public sealed record GapItemDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public RequirementKind Kind { get; init; }

    public string RequirementName { get; init; } = string.Empty;

    public ReadinessStatus Status { get; init; }
}

public sealed record ExpirationsDto
{
    public IReadOnlyList<ExpirationBucketDto> Buckets { get; init; } = [];
}

public sealed record ExpirationBucketDto
{
    public int Days { get; init; }

    public string Label { get; init; } = string.Empty;

    public IReadOnlyList<ExpiringItemDto> Items { get; init; } = [];
}
