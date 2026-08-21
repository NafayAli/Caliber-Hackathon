using Caliber.Api.Domain;

namespace Caliber.Api.Abstractions;

/// <summary>
/// The single identity seam for the whole application.
///
/// Today this is resolved from the demo persona header by middleware. When real
/// authentication is added it will be resolved from claims instead, and nothing
/// else needs to change. For that to hold, <b>no controller or service may read
/// the persona header directly</b> - they depend on this interface only.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The employee this caller is acting as.</summary>
    int EmployeeId { get; }

    string DisplayName { get; }

    AccessLevel AccessLevel { get; }

    /// <summary>The location a manager is scoped to. Admins may access every location.</summary>
    int LocationId { get; }

    /// <summary>
    /// Throws <see cref="Common.ForbiddenException"/> when the caller cannot access the given employee.
    /// Scoping is enforced here, not by post-filtering query results.
    /// </summary>
    void EnsureCanAccessEmployee(int employeeId, int employeeLocationId);
}
