using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Personas;

public sealed record PersonaDto
{
    public int Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public AccessLevel AccessLevel { get; init; }

    public string JobRole { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;
}

public sealed record CurrentUserDto
{
    public int EmployeeId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public AccessLevel AccessLevel { get; init; }

    public int LocationId { get; init; }
}
