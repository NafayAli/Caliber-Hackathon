using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Catalogues;

public sealed record LocationDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;
}

public sealed record DepartmentDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed record GrantedSkillDto
{
    public int SkillId { get; init; }

    public string SkillName { get; init; } = string.Empty;

    public ProficiencyLevel GrantedProficiency { get; init; }
}

public sealed record CertificationDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public CertificationCategory Category { get; init; }

    public string IssuingBody { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int? ValidityMonths { get; init; }

    public int ExpiryWarningDays { get; init; }

    public bool RequiresEvidence { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<GrantedSkillDto> GrantedSkills { get; init; } = [];
}

public sealed record TrainingProgramDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public TrainingCategory Category { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DeliveryMode DeliveryMode { get; init; }

    public decimal EstimatedDurationHours { get; init; }

    public bool RequiresAcknowledgement { get; init; }

    public int? RecurrenceMonths { get; init; }

    public int ExpiryWarningDays { get; init; }

    public bool RequiresEvidence { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<GrantedSkillDto> GrantedSkills { get; init; } = [];
}

public sealed record SkillDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public SkillCategory Category { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}

public sealed record RoleRequirementDto
{
    public int Id { get; init; }

    public RequirementKind Kind { get; init; }

    public int? CertificationId { get; init; }

    public int? TrainingProgramId { get; init; }

    public int? SkillId { get; init; }

    public string Name { get; init; } = string.Empty;

    public ProficiencyLevel? MinimumProficiency { get; init; }

    public bool IsMandatory { get; init; }

    public int? DueWithinDaysOfHire { get; init; }
}

public sealed record JobRoleDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public int DepartmentId { get; init; }

    public IReadOnlyList<RoleRequirementDto> Requirements { get; init; } = [];
}

public sealed record ApplyRoleResultDto
{
    public int CertificationsCreated { get; init; }

    public int TrainingsCreated { get; init; }
}
