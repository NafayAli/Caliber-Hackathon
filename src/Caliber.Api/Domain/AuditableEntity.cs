namespace Caliber.Api.Domain;

/// <summary>
/// Audit stamps applied automatically in <c>CaliberDbContext.SaveChangesAsync</c>.
/// Also lays the groundwork for the audit-log capability the brief lists as a later addition.
/// </summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token so a lost update surfaces as a 409 rather than silently overwriting.</summary>
    public byte[]? RowVersion { get; set; }
}
