using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Evidence;

public sealed record EvidenceDto
{
    public int Id { get; init; }

    public int EmployeeId { get; init; }

    public EvidenceType EvidenceType { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public DateTimeOffset UploadedOn { get; init; }

    public string UploadedBy { get; init; } = string.Empty;

    public bool IsVerified { get; init; }

    public string? VerifiedBy { get; init; }

    public DateTimeOffset? VerifiedOn { get; init; }

    public int? EmployeeCertificationId { get; init; }

    public int? EmployeeTrainingId { get; init; }

    public int? EmployeeSkillId { get; init; }
}

public sealed record EvidenceUploadRequest
{
    public int EmployeeId { get; init; }

    public EvidenceType EvidenceType { get; init; }

    public int? EmployeeCertificationId { get; init; }

    public int? EmployeeTrainingId { get; init; }

    public int? EmployeeSkillId { get; init; }
}

public sealed record VerifyEvidenceRequest
{
    public string? Notes { get; init; }
}
