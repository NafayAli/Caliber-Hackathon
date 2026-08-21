namespace Caliber.Api.Domain;

/// <summary>
/// A supporting document. The file itself lives outside the web root behind
/// <c>IEvidenceStorage</c>; only metadata is stored here.
/// </summary>
public class Evidence : AuditableEntity
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public EvidenceType EvidenceType { get; set; }

    /// <summary>The name the uploader saw. Display only; never used to build a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Server-generated opaque name, so the original name cannot drive path traversal.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset UploadedOn { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>A manager has reviewed this document and attests it supports the claim.</summary>
    public bool IsVerified { get; set; }

    public string? VerifiedBy { get; set; }

    public DateTimeOffset? VerifiedOn { get; set; }

    public int? EmployeeCertificationId { get; set; }

    public EmployeeCertification? EmployeeCertification { get; set; }

    public int? EmployeeTrainingId { get; set; }

    public EmployeeTraining? EmployeeTraining { get; set; }

    public int? EmployeeSkillId { get; set; }

    public EmployeeSkill? EmployeeSkill { get; set; }
}
