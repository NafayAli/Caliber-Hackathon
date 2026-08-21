using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Requests;

public sealed record SkillGrantInput
{
    public int SkillId { get; init; }

    public ProficiencyLevel GrantedProficiency { get; init; }
}

public sealed record SetGrantedSkillsRequest
{
    public IReadOnlyList<SkillGrantInput> Grants { get; init; } = [];
}

public sealed record AssignCertificationRequest
{
    public int CertificationId { get; init; }

    public DateOnly? DueOn { get; init; }

    public string? Notes { get; init; }
}

public sealed record RecordAwardRequest
{
    public DateOnly AwardedOn { get; init; }

    public string? CertificateNumber { get; init; }

    public string? Notes { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record WaiveAssignmentRequest
{
    public string Reason { get; init; } = string.Empty;

    public byte[] RowVersion { get; init; } = [];
}

public sealed record AssignTrainingRequest
{
    public int TrainingProgramId { get; init; }

    public DateOnly? DueOn { get; init; }

    public string? Notes { get; init; }
}

public sealed record UpdateTrainingProgressRequest
{
    public AssignmentStatus? Status { get; init; }

    public int? PercentComplete { get; init; }

    public DateOnly? StartedOn { get; init; }

    public string? Notes { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record CompleteTrainingRequest
{
    public DateOnly? CompletedOn { get; init; }

    public int? Score { get; init; }

    public string? Notes { get; init; }

    public byte[] RowVersion { get; init; } = [];
}

public sealed record AcknowledgeTrainingRequest
{
    public byte[] RowVersion { get; init; } = [];
}

public sealed record AssignSkillRequest
{
    public int SkillId { get; init; }

    public ProficiencyLevel ProficiencyLevel { get; init; }

    public DateOnly? AssessedOn { get; init; }

    public string? Notes { get; init; }
}

public sealed record AddRoleRequirementRequest
{
    public RequirementKind Kind { get; init; }

    public int? CertificationId { get; init; }

    public int? TrainingProgramId { get; init; }

    public int? SkillId { get; init; }

    public ProficiencyLevel? MinimumProficiency { get; init; }

    public bool IsMandatory { get; init; } = true;

    public int? DueWithinDaysOfHire { get; init; }
}

public sealed record CreateJobRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public int DepartmentId { get; init; }
}

public sealed record UpdateJobRoleRequest
{
    public string? Name { get; init; }

    public int? DepartmentId { get; init; }
}

public sealed record CreateCertificationRequest
{
    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public CertificationCategory Category { get; init; }

    public string IssuingBody { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int? ValidityMonths { get; init; }

    public int ExpiryWarningDays { get; init; } = 60;

    public bool RequiresEvidence { get; init; }

    public IReadOnlyList<SkillGrantInput> GrantedSkills { get; init; } = [];
}

public sealed record UpdateCertificationRequest
{
    public string? Name { get; init; }

    public string? Code { get; init; }

    public CertificationCategory? Category { get; init; }

    public string? IssuingBody { get; init; }

    public string? Description { get; init; }

    public int? ValidityMonths { get; init; }

    public int? ExpiryWarningDays { get; init; }

    public bool? RequiresEvidence { get; init; }
}

public sealed record CreateTrainingProgramRequest
{
    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public TrainingCategory Category { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DeliveryMode DeliveryMode { get; init; }

    public decimal EstimatedDurationHours { get; init; }

    public bool RequiresAcknowledgement { get; init; }

    public int? RecurrenceMonths { get; init; }

    public int ExpiryWarningDays { get; init; } = 60;

    public bool RequiresEvidence { get; init; }

    public IReadOnlyList<SkillGrantInput> GrantedSkills { get; init; } = [];
}

public sealed record UpdateTrainingProgramRequest
{
    public string? Name { get; init; }

    public string? Code { get; init; }

    public TrainingCategory? Category { get; init; }

    public string? Provider { get; init; }

    public string? Description { get; init; }

    public DeliveryMode? DeliveryMode { get; init; }

    public decimal? EstimatedDurationHours { get; init; }

    public bool? RequiresAcknowledgement { get; init; }

    public int? RecurrenceMonths { get; init; }

    public int? ExpiryWarningDays { get; init; }

    public bool? RequiresEvidence { get; init; }
}

public sealed record CreateSkillRequest
{
    public string Name { get; init; } = string.Empty;

    public SkillCategory Category { get; init; }

    public string? Description { get; init; }
}

public sealed record UpdateSkillRequest
{
    public string? Name { get; init; }

    public SkillCategory? Category { get; init; }

    public string? Description { get; init; }
}

public sealed record CreateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int JobRoleId { get; init; }

    public int LocationId { get; init; }

    public string? ExternalEmployeeNo { get; init; }

    public DateOnly? HireDate { get; init; }

    public AccessLevel AccessLevel { get; init; } = AccessLevel.Technician;
}

public sealed record UpdateEmployeeRequest
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public int? JobRoleId { get; init; }

    public int? LocationId { get; init; }

    public string? ExternalEmployeeNo { get; init; }

    public DateOnly? HireDate { get; init; }

    public AccessLevel? AccessLevel { get; init; }

    public bool? IsActive { get; init; }
}

public sealed record UpdateProfileRequest
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Phone { get; init; }

    public string? Bio { get; init; }
}

public sealed record BroadcastAnnouncementRequest
{
    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int? LocationId { get; init; }
}

public sealed record NotifyEmployeesRequest
{
    public IReadOnlyList<int> EmployeeIds { get; init; } = [];

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public NotificationKind Kind { get; init; } = NotificationKind.Reminder;
}

public sealed record CreateRenewalRequestBody
{
    public RequirementKind Kind { get; init; }

    public int AssignmentId { get; init; }

    public string? Note { get; init; }
}

public sealed record ReviewRenewalRequestBody
{
    public string? Note { get; init; }
}

public sealed record DirectRenewRequestBody
{
    public RequirementKind Kind { get; init; }

    public int AssignmentId { get; init; }

    public DateOnly? RenewedOn { get; init; }
}
