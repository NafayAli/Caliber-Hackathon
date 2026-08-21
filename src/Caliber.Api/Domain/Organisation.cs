namespace Caliber.Api.Domain;

public class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<JobRole> JobRoles { get; set; } = new List<JobRole>();
}

public class JobRole
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public ICollection<RoleRequirement> Requirements { get; set; } = new List<RoleRequirement>();
}

public class Employee
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Maps to Aspen's <c>AppUser.EmployeeNo</c>. Present from day one so a later
    /// join against the ERP needs no schema change.
    /// </summary>
    public string? ExternalEmployeeNo { get; set; }

    public int JobRoleId { get; set; }

    public JobRole JobRole { get; set; } = null!;

    public int LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    public bool IsActive { get; set; } = true;

    public AccessLevel AccessLevel { get; set; } = AccessLevel.Technician;

    public string? Phone { get; set; }

    public string? Bio { get; set; }

    public string? AvatarFileName { get; set; }

    public UserAccount? UserAccount { get; set; }

    public ICollection<EmployeeCertification> Certifications { get; set; } = new List<EmployeeCertification>();

    public ICollection<EmployeeTraining> Trainings { get; set; } = new List<EmployeeTraining>();

    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();

    public ICollection<Evidence> Evidence { get; set; } = new List<Evidence>();

    public string FullName => $"{FirstName} {LastName}";
}
