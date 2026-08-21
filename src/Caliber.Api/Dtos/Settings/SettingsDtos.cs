using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Settings;

public sealed record AppSettingsDto
{
    public required string ApplicationName { get; init; }

    public required string OrganizationName { get; init; }

    public string? ContactEmail { get; init; }

    public string? SupportPhone { get; init; }

    public string? Tagline { get; init; }

    public string SidebarThemeKey { get; init; } = SidebarThemeKeys.Charcoal;
}

public sealed record UpdateAppSettingsRequest
{
    public required string ApplicationName { get; init; }

    public required string OrganizationName { get; init; }

    public string? ContactEmail { get; init; }

    public string? SupportPhone { get; init; }

    public string? Tagline { get; init; }

    public string? SidebarThemeKey { get; init; }
}

public sealed record ModuleAccessDto
{
    public required AccessLevel AccessLevel { get; init; }

    public required string ModuleKey { get; init; }

    public required bool IsEnabled { get; init; }
}

public sealed record ModuleAccessMatrixDto
{
    public required IReadOnlyList<ModuleAccessDto> Modules { get; init; }
}

public sealed record UpdateModuleAccessRequest
{
    public required AccessLevel AccessLevel { get; init; }

    public required string ModuleKey { get; init; }

    public required bool IsEnabled { get; init; }
}

public sealed record SkillAssignmentRequestDto
{
    public required int Id { get; init; }

    public required int EmployeeId { get; init; }

    public required string EmployeeName { get; init; }

    public required int SkillId { get; init; }

    public required string SkillName { get; init; }

    public required ProficiencyLevel RequestedProficiency { get; init; }

    public required string RequestedByName { get; init; }

    public required DateTimeOffset RequestedAt { get; init; }

    public required SkillRequestStatus Status { get; init; }

    public string? Notes { get; init; }

    public string? ReviewNotes { get; init; }
}

public sealed record CreateSkillAssignmentRequest
{
    public required int SkillId { get; init; }

    public required ProficiencyLevel ProficiencyLevel { get; init; }

    public string? Notes { get; init; }
}

public sealed record ReviewSkillAssignmentRequest
{
    public string? ReviewNotes { get; init; }
}
