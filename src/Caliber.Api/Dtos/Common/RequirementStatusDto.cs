using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Common;

/// <summary>
/// Unified read projection for certifications and training assignments.
/// Status is computed at read time — never persisted.
/// </summary>
public sealed record RequirementStatusDto
{
    public RequirementKind Kind { get; init; }

    public int SourceId { get; init; }

    public int CatalogueId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public AssignmentStatus AssignmentStatus { get; init; }

    public DateOnly? CompletedOn { get; init; }

    /// <summary>ExpiresOn for certifications; NextDueOn for recurring training.</summary>
    public DateOnly? EffectiveDate { get; init; }

    public DateOnly? DueOn { get; init; }

    public int WarningDays { get; init; }

    public ReadinessStatus Status { get; init; }

    public bool IsMandatory { get; init; } = true;

    public byte[] RowVersion { get; init; } = [];

    public bool RequiresAcknowledgement { get; init; }

    public DateTimeOffset? AcknowledgedOn { get; init; }

    public int? PendingRenewalRequestId { get; init; }
}
