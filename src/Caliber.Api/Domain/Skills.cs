namespace Caliber.Api.Domain;

/// <summary>Something an employee can actually do, however they came by it.</summary>
public class Skill
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public SkillCategory Category { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CertificationSkill> GrantedByCertifications { get; set; } = new List<CertificationSkill>();

    public ICollection<TrainingProgramSkill> GrantedByTraining { get; set; } = new List<TrainingProgramSkill>();

    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}

/// <summary>
/// A skill an employee holds. Records where it came from, which is what separates
/// a capability record from a training log: a skill may be granted automatically by
/// completing a certification or training, or assessed from real-world experience.
/// </summary>
public class EmployeeSkill : AuditableEntity
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public ProficiencyLevel ProficiencyLevel { get; set; } = ProficiencyLevel.Beginner;

    public SkillSourceType SourceType { get; set; } = SkillSourceType.ManagerAssessed;

    /// <summary>Set when the skill was granted automatically by completing a certification.</summary>
    public int? SourceCertificationId { get; set; }

    public Certification? SourceCertification { get; set; }

    /// <summary>Set when the skill was granted automatically by completing a training program.</summary>
    public int? SourceTrainingProgramId { get; set; }

    public TrainingProgram? SourceTrainingProgram { get; set; }

    public DateOnly AssessedOn { get; set; }

    public string AssessedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    public EmployeeSkillStatus Status { get; set; } = EmployeeSkillStatus.Active;

    public int? SourceEmployeeCertificationId { get; set; }

    public EmployeeCertification? SourceEmployeeCertification { get; set; }

    public int? SourceEmployeeTrainingId { get; set; }

    public EmployeeTraining? SourceEmployeeTraining { get; set; }

    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();
}
