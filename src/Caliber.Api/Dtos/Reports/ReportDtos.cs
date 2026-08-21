using Caliber.Api.Domain;
using Caliber.Api.Dtos.Dashboard;

namespace Caliber.Api.Dtos.Reports;

public sealed record ReadinessSummaryReportDto
{
    public decimal OverallCompliancePercent { get; init; }

    public int TotalEmployees { get; init; }

    public int EmployeesFullyReady { get; init; }

    public IReadOnlyList<EmployeeReadinessSummaryDto> Employees { get; init; } = [];
}

public sealed record EmployeeReadinessSummaryDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public decimal ReadinessPercent { get; init; }

    public ReadinessStatus WorstStatus { get; init; }

    public int GapCount { get; init; }
}

public sealed record ExpirationScheduleReportDto
{
    public int TotalExpiring { get; init; }

    public IReadOnlyList<ExpirationBucketDto> Buckets { get; init; } = [];
}

public sealed record ComplianceGapsReportDto
{
    public int TotalGaps { get; init; }

    public IReadOnlyList<GapItemDto> Gaps { get; init; } = [];
}

public sealed record SkillsMatrixReportDto
{
    public IReadOnlyList<SkillColumnDto> Skills { get; init; } = [];

    public IReadOnlyList<SkillsMatrixRowDto> Rows { get; init; } = [];
}

public sealed record SkillColumnDto
{
    public int SkillId { get; init; }

    public string SkillName { get; init; } = string.Empty;

    public SkillCategory Category { get; init; }
}

public sealed record SkillsMatrixRowDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public IReadOnlyList<SkillsMatrixCellDto> Cells { get; init; } = [];
}

public sealed record SkillsMatrixCellDto
{
    public int SkillId { get; init; }

    public ProficiencyLevel? ProficiencyLevel { get; init; }
}

public sealed record AtRiskEmployeesReportDto
{
    public int TotalAtRisk { get; init; }

    public int CriticalCount { get; init; }

    public decimal AvgReadinessPercent { get; init; }

    public IReadOnlyList<AtRiskEmployeeRowDto> Employees { get; init; } = [];
}

public sealed record AtRiskEmployeeRowDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public decimal ReadinessPercent { get; init; }

    public int ExpiredCount { get; init; }

    public int OverdueCount { get; init; }

    public int MissingCount { get; init; }

    public ReadinessStatus WorstStatus { get; init; }

    public string TopGapName { get; init; } = string.Empty;

    public int RiskScore { get; init; }
}

public sealed record ComplianceLeadersReportDto
{
    public int FullyReadyCount { get; init; }

    public int GoldCount { get; init; }

    public int SilverCount { get; init; }

    public int ReadyCount { get; init; }

    public decimal WorkforceReadyPercent { get; init; }

    public IReadOnlyList<ComplianceLeaderRowDto> Leaders { get; init; } = [];
}

public sealed record ComplianceLeaderRowDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public string Tier { get; init; } = string.Empty;

    public decimal ReadinessPercent { get; init; }

    public ReadinessStatus WorstStatus { get; init; }
}

public sealed record LocationScorecardReportDto
{
    public decimal OrgCompliancePercent { get; init; }

    public string? TopLocationName { get; init; }

    public string? BottomLocationName { get; init; }

    public IReadOnlyList<LocationScorecardRowDto> Locations { get; init; } = [];
}

public sealed record LocationScorecardRowDto
{
    public int LocationId { get; init; }

    public string LocationName { get; init; } = string.Empty;

    public int Rank { get; init; }

    public int EmployeeCount { get; init; }

    public int FullyReadyCount { get; init; }

    public int AtRiskCount { get; init; }

    public int ExpiringSoonCount { get; init; }

    public decimal CompliancePercent { get; init; }

    public decimal FullyReadyPercent { get; init; }
}
