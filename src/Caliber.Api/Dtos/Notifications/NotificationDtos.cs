using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Notifications;

public sealed record NotificationDto
{
    public int Id { get; init; }

    public NotificationKind Kind { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public int? RelatedEmployeeId { get; init; }

    public RequirementKind? RelatedKind { get; init; }

    public int? RelatedAssignmentId { get; init; }

    public int? RenewalRequestId { get; init; }

    public string? CreatedByName { get; init; }
}

public sealed record NotificationSummaryDto
{
    public int UnreadCount { get; init; }

    public IReadOnlyList<NotificationDto> Items { get; init; } = [];
}

public sealed record RenewalRequestDto
{
    public int Id { get; init; }

    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    public RequirementKind Kind { get; init; }

    public int AssignmentId { get; init; }

    public string RequirementName { get; init; } = string.Empty;

    public RenewalRequestStatus Status { get; init; }

    public string? EmployeeNote { get; init; }

    public string? ReviewerNote { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }
}
