using Caliber.Api.Data;
using Caliber.Api.Dtos.Auth;
using Caliber.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await auth.LoginAsync(request, cancellationToken);
        await SignInUserAsync(user);
        return Ok(user);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await auth.RegisterAsync(request, cancellationToken);
        await SignInUserAsync(user);
        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var employeeIdClaim = User.FindFirst(Security.AuthClaimTypes.EmployeeId)?.Value;
        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized();
        }

        return Ok(await auth.GetMeAsync(employeeId, cancellationToken));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var employeeIdClaim = User.FindFirst(Security.AuthClaimTypes.EmployeeId)?.Value;
        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized();
        }

        await auth.ChangePasswordAsync(employeeId, request, cancellationToken);
        return NoContent();
    }

    private async Task SignInUserAsync(AuthUserDto user)
    {
        var principal = AuthService.CreatePrincipal(
            new Domain.Employee
            {
                Id = user.EmployeeId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessLevel = user.AccessLevel,
                LocationId = user.LocationId,
            },
            user.Email);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            AuthService.CreateAuthProperties());
    }
}

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(CaliberDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var locations = await db.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Code = l.Code,
                City = l.City,
            })
            .ToListAsync(cancellationToken);

        return Ok(locations);
    }
}
