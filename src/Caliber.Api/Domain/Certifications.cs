namespace Caliber.Api.Domain;

/// <summary>A credential issued by an external body, with an expiry and a renewal cycle.</summary>
public class Certification
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public CertificationCategory Category { get; set; }

    public string IssuingBody { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Null means the certification never expires.</summary>
    public int? ValidityMonths { get; set; }

    /// <summary>How far ahead of expiry an item starts reading as "expiring soon".</summary>
    public int ExpiryWarningDays { get; set; } = 60;

    public bool RequiresEvidence { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CertificationSkill> GrantedSkills { get; set; } = new List<CertificationSkill>();

    public ICollection<EmployeeCertification> Assignments { get; set; } = new List<EmployeeCertification>();
}

/// <summary>Holding this certification credits the employee with this skill.</summary>
public class CertificationSkill
{
    public int CertificationId { get; set; }

    public Certification Certification { get; set; } = null!;

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public ProficiencyLevel GrantedProficiency { get; set; } = ProficiencyLevel.Intermediate;
}

/// <summary>One employee's obligation to hold one certification.</summary>
public class EmployeeCertification : AuditableEntity
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int CertificationId { get; set; }

    public Certification Certification { get; set; } = null!;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.NotStarted;

    public AssignmentSource Source { get; set; } = AssignmentSource.Direct;

    public DateOnly AssignedOn { get; set; }

    public DateOnly? DueOn { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Append-only renewal history. Current award and expiry are the most recent
    /// entry, which keeps a full audit trail rather than overwriting a single date.
    /// </summary>
    public ICollection<CertificationAward> Awards { get; set; } = new List<CertificationAward>();

    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();
}

/// <summary>A single grant or renewal of a certification.</summary>
public class CertificationAward
{
    public int Id { get; set; }

    public int EmployeeCertificationId { get; set; }

    public EmployeeCertification EmployeeCertification { get; set; } = null!;

    public DateOnly AwardedOn { get; set; }

    /// <summary>Computed from <c>AwardedOn</c> plus the certification's validity; null if it never expires.</summary>
    public DateOnly? ExpiresOn { get; set; }

    public string? CertificateNumber { get; set; }

    public string RecordedBy { get; set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; set; }

    public string? Notes { get; set; }
}
