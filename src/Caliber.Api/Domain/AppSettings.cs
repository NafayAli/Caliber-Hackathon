namespace Caliber.Api.Domain;

/// <summary>Singleton organization settings row.</summary>
public class AppSettings
{
    public int Id { get; set; }

    public string ApplicationName { get; set; } = "Caliber";

    public string OrganizationName { get; set; } = "Constellation Dealer";

    public string? ContactEmail { get; set; }

    public string? SupportPhone { get; set; }

    public string? Tagline { get; set; } = "Calibration & Performance";

    /// <summary>Key for navigation panel color preset (see SidebarThemeKeys).</summary>
    public string SidebarThemeKey { get; set; } = SidebarThemeKeys.Charcoal;
}

public static class SidebarThemeKeys
{
    public const string Charcoal = "charcoal";
    public const string Forest = "forest";
    public const string Slate = "slate";
    public const string Plum = "plum";
    public const string Espresso = "espresso";
    public const string DeepTeal = "deepTeal";
    public const string Wine = "wine";
    public const string Graphite = "graphite";
    public const string Mustard = "mustard";
    public const string Chocolate = "chocolate";
    public const string Orange = "orange";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Charcoal, Forest, Slate, Plum, Espresso, DeepTeal, Wine, Graphite, Mustard, Chocolate, Orange,
    };
}

/// <summary>Per-role module visibility for navigation and API gating.</summary>
public class RoleModuleAccess
{
    public int Id { get; set; }

    public AccessLevel AccessLevel { get; set; }

    public string ModuleKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
}

public static class ModuleKeys
{
    public const string Dashboard = "Dashboard";
    public const string Employees = "Employees";
    public const string Users = "Users";
    public const string Certifications = "Certifications";
    public const string Training = "Training";
    public const string Skills = "Skills";
    public const string Roles = "Roles";
    public const string Expirations = "Expirations";
    public const string Reports = "Reports";
    public const string Settings = "Settings";
    public const string MyRequirements = "MyRequirements";
    public const string Profile = "Profile";
    public const string About = "About";

    public static readonly string[] All =
    [
        Dashboard, Employees, Users, Certifications, Training, Skills, Roles,
        Expirations, Reports, Settings, MyRequirements, Profile, About,
    ];
}
