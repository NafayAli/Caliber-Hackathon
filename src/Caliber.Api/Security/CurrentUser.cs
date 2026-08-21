using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Domain;

namespace Caliber.Api.Security;

/// <summary>
/// Scoped, mutable holder for the caller's identity. Populated once per request by
/// <see cref="PersonaMiddleware"/> and read everywhere else through
/// <see cref="ICurrentUser"/>.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; private set; }

    public int EmployeeId { get; private set; }

    public string DisplayName { get; private set; } = "anonymous";

    public AccessLevel AccessLevel { get; private set; } = AccessLevel.Technician;

    public int LocationId { get; private set; }

    internal void Resolve(int employeeId, string displayName, AccessLevel accessLevel, int locationId)
    {
        IsAuthenticated = true;
        EmployeeId = employeeId;
        DisplayName = displayName;
        AccessLevel = accessLevel;
        LocationId = locationId;
    }

    public void EnsureCanAccessEmployee(int employeeId, int employeeLocationId)
    {
        if (!IsAuthenticated)
        {
            throw new ForbiddenException();
        }

        switch (AccessLevel)
        {
            case AccessLevel.Admin:
                return;
            case AccessLevel.Manager:
                if (employeeLocationId != LocationId)
                {
                    throw new ForbiddenException("Managers may only access employees at their own location.");
                }

                return;
            case AccessLevel.Technician:
                if (employeeId != EmployeeId)
                {
                    throw new ForbiddenException("Technicians may only access their own record.");
                }

                return;
            default:
                throw new ForbiddenException();
        }
    }
}
