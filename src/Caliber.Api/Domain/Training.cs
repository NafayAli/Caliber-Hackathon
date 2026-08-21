namespace Caliber.Api.Domain;

/// <summary>
/// A course with content and progress. Deliberately a separate aggregate from
/// <see cref="Certification"/>: training has a provider, a delivery mode, modules,
/// an optional acknowledgement, and recurs rather than expiring.
/// </summary>
public class TrainingProgram
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public TrainingCategory Category { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DeliveryMode DeliveryMode { get; set; }

    public decimal EstimatedDurationHours { get; set; }

    /// <summary>Employee must attest they have read and understood the material.</summary>
    public bool RequiresAcknowledgement { get; set; }

    /// <summary>Null means one-time. Otherwise it recurs, e.g. an annual safety refresher.</summary>
    public int? RecurrenceMonths { get; set; }

    public int ExpiryWarningDays { get; set; } = 60;

    public bool RequiresEvidence { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Retained in the schema even though module-level progress tracking was cut
    /// from scope, so the capability is not designed out.
    /// </summary>
    public ICollection<TrainingModule> Modules { get; set; } = new List<TrainingModule>();

    public ICollection<TrainingProgramSkill> GrantedSkills { get; set; } = new List<TrainingProgramSkill>();

    public ICollection<EmployeeTraining> Assignments { get; set; } = new List<EmployeeTraining>();
}

public class TrainingModule
{
    public int Id { get; set; }

    public int TrainingProgramId { get; set; }

    public TrainingProgram TrainingProgram { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public decimal EstimatedDurationHours { get; set; }
}

/// <summary>Completing this training credits the employee with this skill.</summary>
public class TrainingProgramSkill
{
    public int TrainingProgramId { get; set; }

    public TrainingProgram TrainingProgram { get; set; } = null!;

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public ProficiencyLevel GrantedProficiency { get; set; } = ProficiencyLevel.Beginner;
}

/// <summary>One employee's obligation to complete one training program.</summary>
public class EmployeeTraining : AuditableEntity
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int TrainingProgramId { get; set; }

    public TrainingProgram TrainingProgram { get; set; } = null!;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.NotStarted;

    public AssignmentSource Source { get; set; } = AssignmentSource.Direct;

    public DateOnly AssignedOn { get; set; }

    public DateOnly? DueOn { get; set; }

    public DateOnly? StartedOn { get; set; }

    public DateOnly? CompletedOn { get; set; }

    /// <summary>Computed from <c>CompletedOn</c> plus the program's recurrence; null if one-time.</summary>
    public DateOnly? NextDueOn { get; set; }

    public DateTimeOffset? AcknowledgedOn { get; set; }

    public string? AcknowledgedBy { get; set; }

    public int? Score { get; set; }

    /// <summary>0-100. Stands in for per-module records, which were cut from scope.</summary>
    public int PercentComplete { get; set; }

    public string? Notes { get; set; }

    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();
}
