namespace Caliber.Api.Domain;

/// <summary>
/// What a job role obliges its holders to have. Applying a role's template to an
/// employee generates their onboarding checklist.
/// </summary>
public class RoleRequirement
{
    public int Id { get; set; }

    public int JobRoleId { get; set; }

    public JobRole JobRole { get; set; } = null!;

    public RequirementKind Kind { get; set; }

    public int? CertificationId { get; set; }

    public Certification? Certification { get; set; }

    public int? TrainingProgramId { get; set; }

    public TrainingProgram? TrainingProgram { get; set; }

    public int? SkillId { get; set; }

    public Skill? Skill { get; set; }

    /// <summary>Only meaningful when <see cref="Kind"/> is <see cref="RequirementKind.Skill"/>.</summary>
    public ProficiencyLevel? MinimumProficiency { get; set; }

    public bool IsMandatory { get; set; } = true;

    /// <summary>Drives a real due date on a new hire's checklist rather than an open-ended one.</summary>
    public int? DueWithinDaysOfHire { get; set; }
}
