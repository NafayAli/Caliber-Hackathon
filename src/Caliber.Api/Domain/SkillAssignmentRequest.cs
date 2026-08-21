namespace Caliber.Api.Domain;

public class SkillAssignmentRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public ProficiencyLevel RequestedProficiency { get; set; }

    public int RequestedByEmployeeId { get; set; }

    public Employee RequestedBy { get; set; } = null!;

    public DateTimeOffset RequestedAt { get; set; }

    public SkillRequestStatus Status { get; set; } = SkillRequestStatus.Pending;

    public int? ReviewedByEmployeeId { get; set; }

    public Employee? ReviewedBy { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public string? Notes { get; set; }
}
