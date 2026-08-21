using Caliber.Api.Domain;
using Caliber.Api.Dtos.Common;
using Caliber.Api.Dtos.Evidence;

namespace Caliber.Api.Dtos.Employees;

public sealed record EmployeeListQuery
{
    public int Offset { get; init; }

    public int Limit { get; init; } = 50;

    public int? LocationId { get; init; }

    public int? JobRoleId { get; init; }

    public ReadinessStatus? Status { get; init; }

    public string? Search { get; init; }
}

public sealed record EmployeeListItemDto
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string JobRole { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public decimal ReadinessPercent { get; init; }

    public ReadinessStatus WorstStatus { get; init; }
}

public sealed record EmployeeProfileDto
{
    public int Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? ExternalEmployeeNo { get; init; }

    public string JobRole { get; init; } = string.Empty;

    public int JobRoleId { get; init; }

    public string Location { get; init; } = string.Empty;

    public int LocationId { get; init; }

    public DateOnly HireDate { get; init; }

    public AccessLevel AccessLevel { get; init; }

    public decimal ReadinessPercent { get; init; }

    public IReadOnlyList<RequirementStatusDto> Requirements { get; init; } = [];

    public IReadOnlyList<EmployeeSkillDto> Skills { get; init; } = [];

    public IReadOnlyList<EvidenceDto> Evidence { get; init; } = [];
}

public sealed record EmployeeCertificationDto
{
    public int Id { get; init; }

    public int CertificationId { get; init; }

    public string CertificationName { get; init; } = string.Empty;

    public string CertificationCode { get; init; } = string.Empty;

    public AssignmentStatus Status { get; init; }

    public AssignmentSource Source { get; init; }

    public DateOnly AssignedOn { get; init; }

    public DateOnly? DueOn { get; init; }

    public string? Notes { get; init; }

    public ReadinessStatus ReadinessStatus { get; init; }

    public CertificationAwardDto? LatestAward { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record CertificationAwardDto
{
    public int Id { get; init; }

    public DateOnly AwardedOn { get; init; }

    public DateOnly? ExpiresOn { get; init; }

    public string? CertificateNumber { get; init; }

    public string RecordedBy { get; init; } = string.Empty;

    public DateTimeOffset RecordedAt { get; init; }

    public string? Notes { get; init; }
}

public sealed record EmployeeTrainingDto
{
    public int Id { get; init; }

    public int TrainingProgramId { get; init; }

    public string TrainingProgramName { get; init; } = string.Empty;

    public string TrainingProgramCode { get; init; } = string.Empty;

    public AssignmentStatus Status { get; init; }

    public AssignmentSource Source { get; init; }

    public DateOnly AssignedOn { get; init; }

    public DateOnly? DueOn { get; init; }

    public DateOnly? StartedOn { get; init; }

    public DateOnly? CompletedOn { get; init; }

    public DateOnly? NextDueOn { get; init; }

    public DateTimeOffset? AcknowledgedOn { get; init; }

    public string? AcknowledgedBy { get; init; }

    public int PercentComplete { get; init; }

    public int? Score { get; init; }

    public string? Notes { get; init; }

    public ReadinessStatus ReadinessStatus { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record EmployeeSkillDto
{
    public int Id { get; init; }

    public int SkillId { get; init; }

    public string SkillName { get; init; } = string.Empty;

    public SkillCategory Category { get; init; }

    public ProficiencyLevel ProficiencyLevel { get; init; }

    public SkillSourceType SourceType { get; init; }

    public int? SourceCertificationId { get; init; }

    public int? SourceTrainingProgramId { get; init; }

    public DateOnly AssessedOn { get; init; }

    public string AssessedBy { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public DateOnly? ExpiresOn { get; init; }

    public EmployeeSkillStatus Status { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
