namespace Caliber.Api.Domain;

public class UserAccount
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
