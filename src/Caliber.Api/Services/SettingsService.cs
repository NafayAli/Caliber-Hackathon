using Caliber.Api.Abstractions;
using Caliber.Api.Common;
using Caliber.Api.Data;
using Caliber.Api.Domain;
using Caliber.Api.Dtos.Settings;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Services;

public sealed class SettingsService(CaliberDbContext db, ICurrentUser currentUser)
{
    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<AppSettingsDto> UpdateSettingsAsync(
        UpdateAppSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.ApplicationName = request.ApplicationName.Trim();
        settings.OrganizationName = request.OrganizationName.Trim();
        if (request.ContactEmail is not null)
        {
            settings.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail)
                ? null
                : request.ContactEmail.Trim().ToLowerInvariant();
        }

        settings.SupportPhone = string.IsNullOrWhiteSpace(request.SupportPhone)
            ? null
            : request.SupportPhone.Trim();
        settings.Tagline = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim();

        var themeKey = request.SidebarThemeKey?.Trim();
        if (string.IsNullOrWhiteSpace(themeKey))
        {
            settings.SidebarThemeKey = SidebarThemeKeys.Charcoal;
        }
        else
        {
            var canonicalTheme = SidebarThemeKeys.All.FirstOrDefault(
                key => string.Equals(key, themeKey, StringComparison.OrdinalIgnoreCase));
            settings.SidebarThemeKey = canonicalTheme ?? SidebarThemeKeys.Charcoal;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<ModuleAccessMatrixDto> GetModuleAccessAsync(CancellationToken cancellationToken = default)
    {
        await EnsureModuleAccessSeededAsync(cancellationToken);

        var rows = await db.RoleModuleAccess
            .AsNoTracking()
            .OrderBy(x => x.AccessLevel)
            .ThenBy(x => x.ModuleKey)
            .Select(x => new ModuleAccessDto
            {
                AccessLevel = x.AccessLevel,
                ModuleKey = x.ModuleKey,
                IsEnabled = x.IsEnabled,
            })
            .ToListAsync(cancellationToken);

        return new ModuleAccessMatrixDto { Modules = rows };
    }

    public async Task<ModuleAccessMatrixDto> GetModuleAccessForCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureModuleAccessSeededAsync(cancellationToken);

        if (!currentUser.IsAuthenticated)
        {
            return new ModuleAccessMatrixDto { Modules = [] };
        }

        var level = currentUser.AccessLevel;
        var rows = await db.RoleModuleAccess
            .AsNoTracking()
            .Where(x => x.AccessLevel == level && x.IsEnabled)
            .Select(x => new ModuleAccessDto
            {
                AccessLevel = x.AccessLevel,
                ModuleKey = x.ModuleKey,
                IsEnabled = x.IsEnabled,
            })
            .ToListAsync(cancellationToken);

        return new ModuleAccessMatrixDto { Modules = rows };
    }

    public async Task UpdateModuleAccessAsync(
        UpdateModuleAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManagerOrAdmin();

        if (!ModuleKeys.All.Contains(request.ModuleKey))
        {
            throw new BadRequestException($"Unknown module key '{request.ModuleKey}'.");
        }

        if (request.AccessLevel == AccessLevel.Admin
            && request.ModuleKey == ModuleKeys.Settings
            && !request.IsEnabled
            && currentUser.AccessLevel != AccessLevel.Admin)
        {
            throw new ForbiddenException("Only administrators may disable admin settings access.");
        }

        await EnsureModuleAccessSeededAsync(cancellationToken);

        var row = await db.RoleModuleAccess
            .FirstOrDefaultAsync(
                x => x.AccessLevel == request.AccessLevel && x.ModuleKey == request.ModuleKey,
                cancellationToken)
            ?? throw new NotFoundException("RoleModuleAccess", $"{request.AccessLevel}:{request.ModuleKey}");

        row.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureModuleAccessSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.RoleModuleAccess.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var level in new[] { AccessLevel.Admin, AccessLevel.Manager, AccessLevel.Technician })
        {
            foreach (var moduleKey in ModuleKeys.All)
            {
                db.RoleModuleAccess.Add(new RoleModuleAccess
                {
                    AccessLevel = level,
                    ModuleKey = moduleKey,
                    IsEnabled = IsDefaultModuleEnabled(level, moduleKey),
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsDefaultModuleEnabled(AccessLevel level, string moduleKey) =>
        level switch
        {
            AccessLevel.Admin => true,
            AccessLevel.Manager => moduleKey is not ModuleKeys.Roles,
            AccessLevel.Technician => moduleKey is ModuleKeys.MyRequirements
                or ModuleKeys.Profile
                or ModuleKeys.About,
            _ => false,
        };

    private async Task<AppSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.AppSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppSettings();
        db.AppSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static AppSettingsDto MapSettings(AppSettings settings) =>
        new()
        {
            ApplicationName = settings.ApplicationName,
            OrganizationName = settings.OrganizationName,
            ContactEmail = settings.ContactEmail,
            SupportPhone = settings.SupportPhone,
            Tagline = settings.Tagline,
            SidebarThemeKey = string.IsNullOrWhiteSpace(settings.SidebarThemeKey)
                ? SidebarThemeKeys.Charcoal
                : settings.SidebarThemeKey,
        };

    private void EnsureManagerOrAdmin()
    {
        if (currentUser.AccessLevel is not AccessLevel.Manager and not AccessLevel.Admin)
        {
            throw new ForbiddenException("Only managers and administrators may change settings.");
        }
    }
}
