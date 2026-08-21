using Caliber.Api.Abstractions;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Security;

/// <summary>
/// Resolves the caller from the auth cookie and optionally allows Admin impersonation
/// via the <see cref="HeaderName"/> header.
/// </summary>
public sealed class PersonaMiddleware
{
    public const string HeaderName = "X-Persona-Id";

    private readonly RequestDelegate _next;

    public PersonaMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser, CaliberDbContext db)
    {
        if (!RequiresAuth(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await WriteUnauthorised(context, "Not signed in.");
            return;
        }

        if (!int.TryParse(context.User.FindFirst(AuthClaimTypes.EmployeeId)?.Value, out var employeeId))
        {
            await WriteUnauthorised(context, "Invalid session.");
            return;
        }

        var sessionAccessLevel = ParseAccessLevel(context.User.FindFirst(AuthClaimTypes.AccessLevel)?.Value);

        if (sessionAccessLevel == AccessLevel.Admin
            && context.Request.Headers.TryGetValue(HeaderName, out var raw)
            && int.TryParse(raw.ToString(), out var impersonateId))
        {
            employeeId = impersonateId;
        }

        var persona = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.AccessLevel,
                e.LocationId,
            })
            .FirstOrDefaultAsync(context.RequestAborted);

        if (persona is null)
        {
            await WriteUnauthorised(context, "The signed-in user does not match an active employee.");
            return;
        }

        currentUser.Resolve(
            persona.Id,
            $"{persona.FirstName} {persona.LastName}",
            persona.AccessLevel,
            persona.LocationId);

        await _next(context);
    }

    private static AccessLevel ParseAccessLevel(string? value) =>
        Enum.TryParse<AccessLevel>(value, out var level) ? level : AccessLevel.Technician;

    private static bool RequiresAuth(PathString path)
    {
        if (!path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (path.StartsWithSegments("/api/health"))
        {
            return false;
        }

        if (path.StartsWithSegments("/api/auth/login") || path.StartsWithSegments("/api/auth/register"))
        {
            return false;
        }

        if (path.StartsWithSegments("/api/locations") && path.Value?.EndsWith("/locations", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        if (path.StartsWithSegments("/api/job-roles") && !path.Value!.Contains("/requirements", StringComparison.Ordinal) && !path.Value.Contains("/apply", StringComparison.Ordinal))
        {
            var segments = path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
            {
                return false;
            }

            if (segments.Length == 3 && int.TryParse(segments[2], out _))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task WriteUnauthorised(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            title = "Not signed in",
            status = StatusCodes.Status401Unauthorized,
            detail,
            traceId = context.TraceIdentifier,
        });
    }
}
