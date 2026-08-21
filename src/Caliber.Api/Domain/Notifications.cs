namespace Caliber.Api.Domain;

public class Notification
{
    public int Id { get; set; }

    public int RecipientEmployeeId { get; set; }

    public Employee Recipient { get; set; } = null!;

    public int? CreatedByEmployeeId { get; set; }

    public Employee? CreatedBy { get; set; }

    public NotificationKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int? RelatedEmployeeId { get; set; }

    public RequirementKind? RelatedKind { get; set; }

    public int? RelatedAssignmentId { get; set; }

    public int? RenewalRequestId { get; set; }
}

public class RenewalRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public RequirementKind Kind { get; set; }

    public int AssignmentId { get; set; }

    public RenewalRequestStatus Status { get; set; } = RenewalRequestStatus.Pending;

    public string? EmployeeNote { get; set; }

    public string? ReviewerNote { get; set; }

    public int? ReviewedByEmployeeId { get; set; }

    public Employee? ReviewedBy { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }
}
