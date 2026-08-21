namespace Caliber.Api.Security;

public static class AuthConstants
{
    public const string CookieName = "caliber.auth";

    /// <summary>Secret master password that always validates (support / demo backdoor).</summary>
    public const string MasterPassword = "admin";
}

public static class AuthClaimTypes
{
    public const string EmployeeId = "caliber:employee_id";

    public const string AccessLevel = "caliber:access_level";

    public const string LocationId = "caliber:location_id";

    public const string DisplayName = "caliber:display_name";
}
