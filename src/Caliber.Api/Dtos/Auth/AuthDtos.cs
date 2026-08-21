using Caliber.Api.Domain;

namespace Caliber.Api.Dtos.Auth;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public int LocationId { get; init; }

    public int JobRoleId { get; init; }
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class AuthUserDto
{
    public int EmployeeId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public AccessLevel AccessLevel { get; init; }

    public int LocationId { get; init; }

    public string LocationName { get; init; } = string.Empty;

    public string JobRoleName { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Bio { get; init; }

    public string? AvatarUrl { get; init; }
}

public sealed class LocationDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;
}
