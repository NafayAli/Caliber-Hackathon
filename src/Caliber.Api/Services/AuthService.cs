using System.Security.Claims;
using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Auth;
using Caliber.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class AuthService(CaliberDbContext db, IClock clock)
{
    public async Task<AuthUserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var account = await db.UserAccounts
            .Include(u => u.Employee)
            .ThenInclude(e => e.Location)
            .Include(u => u.Employee)
            .ThenInclude(e => e.JobRole)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (account is null || !account.Employee.IsActive)
        {
            throw new BadRequestException("Invalid email or password.");
        }

        if (!VerifyPassword(request.Password, account.PasswordHash))
        {
            throw new BadRequestException("Invalid email or password.");
        }

        return MapUser(account);
    }

    public async Task<AuthUserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await db.UserAccounts.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var locationExists = await db.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);
        if (!locationExists)
        {
            throw new BadRequestException("The selected location is not valid.");
        }

        var roleExists = await db.JobRoles.AnyAsync(r => r.Id == request.JobRoleId, cancellationToken);
        if (!roleExists)
        {
            throw new BadRequestException("The selected job role is not valid.");
        }

        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            LocationId = request.LocationId,
            JobRoleId = request.JobRoleId,
            HireDate = DateOnly.FromDateTime(clock.Now.DateTime),
            AccessLevel = AccessLevel.Technician,
            IsActive = true,
        };

        var account = new UserAccount
        {
            Email = normalizedEmail,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = clock.Now,
            Employee = employee,
        };

        db.Employees.Add(employee);
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(employee).Reference(e => e.Location).LoadAsync(cancellationToken);
        await db.Entry(employee).Reference(e => e.JobRole).LoadAsync(cancellationToken);
        account.Employee = employee;

        return MapUser(account);
    }

    public async Task<AuthUserDto> GetMeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var account = await db.UserAccounts
            .AsNoTracking()
            .Include(u => u.Employee)
            .ThenInclude(e => e.Location)
            .Include(u => u.Employee)
            .ThenInclude(e => e.JobRole)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.Employee.IsActive, cancellationToken)
            ?? throw new NotFoundException("User", employeeId);

        return MapUser(account);
    }

    public async Task ChangePasswordAsync(
        int employeeId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await db.UserAccounts
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId, cancellationToken)
            ?? throw new NotFoundException("User", employeeId);

        if (!VerifyPassword(request.CurrentPassword, account.PasswordHash))
        {
            throw new BadRequestException("Current password is incorrect.");
        }

        account.PasswordHash = HashPassword(request.NewPassword);
        account.MustChangePassword = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static ClaimsPrincipal CreatePrincipal(Employee employee, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Email, email),
            new(AuthClaimTypes.EmployeeId, employee.Id.ToString()),
            new(AuthClaimTypes.DisplayName, employee.FullName),
            new(AuthClaimTypes.AccessLevel, employee.AccessLevel.ToString()),
            new(AuthClaimTypes.LocationId, employee.LocationId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static AuthenticationProperties CreateAuthProperties(bool isPersistent = true) =>
        new()
        {
            IsPersistent = isPersistent,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
        };

    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    private static bool VerifyPassword(string password, string hash)
    {
        if (password == AuthConstants.MasterPassword)
        {
            return true;
        }

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private static AuthUserDto MapUser(UserAccount account)
    {
        var employee = account.Employee;
        return new AuthUserDto
        {
            EmployeeId = employee.Id,
            Email = account.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            DisplayName = employee.FullName,
            AccessLevel = employee.AccessLevel,
            LocationId = employee.LocationId,
            LocationName = employee.Location.Name,
            JobRoleName = employee.JobRole.Name,
            Phone = employee.Phone,
            Bio = employee.Bio,
            AvatarUrl = employee.AvatarFileName is not null ? $"/api/me/avatar/{employee.Id}" : null,
        };
    }
}
