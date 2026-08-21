# Caliber — Technical Implementation Guide

## Purpose

This document provides a complete technical implementation guide for **Caliber** (`Caliber.Api` + `web`) — a standalone workforce readiness web application that tracks dealership employee certifications, training, skills, and supporting evidence. The API is an ASP.NET Core 8 Web API backed by SQL Server via EF Core; the client is a React 19 + TypeScript SPA built with Vite.

Caliber's organising principle is **separate write models, one unified read projection**. Certifications and training are distinct aggregates with different lifecycles; `ReadinessService` projects both into a single `RequirementStatus` so the dashboard, employee list, profile, and expirations screens are each written once.

This document is intended to be consumed by a developer or copilot agent to plan and implement the application. The authoritative design reference is [solution.md](./solution.md).

**Current state (2026-08-21):**

- Full implementation complete including **cookie authentication**, user management, **7-report** module, catalogue edit/deactivate (incl. skills), navy/teal rebrand, About page, **Settings module** (sidebar themes), dashboard analytics charts, **CAL-053–062 final polish**, and **round 3**: in-app **notifications** (renewal Accept/Reject in panel), **renewal requests**, **granted-skills editor**, login redirect fix, **report headers** (org name + title), **fully-ready KPI alignment**.
- Migrations: `InitialSchema`, `AddUserAccountsAndProfileFields`, `SettingsAndSkillLifecycle`, `NotificationsAndRenewals`.
- **AppSettings** + **RoleModuleAccess** for org branding and per-role module visibility.
- **SkillAssignmentRequest** for pending manual skill approval; skills grant from cert/training with `ExpiresOn`.
- Seed backfills `UserAccount` per missing employee on every startup; stale demo DB auto-recreate in Development.

---

## Technology Stack

| Component | Technology |
| --- | --- |
| API framework | ASP.NET Core 8 Web API |
| Language (API) | C# 12, `net8.0` |
| Language (client) | TypeScript 5.9 |
| Build (API) | `dotnet` / MSBuild |
| Build (client) | Vite 8 |
| Database | SQL Server (default `(localdb)\MSSQLLocalDB`, database `Caliber`) |
| ORM | EF Core 8 |
| Validation | FluentValidation |
| Logging | Serilog |
| API docs | Swashbuckle (Swagger / OpenAPI) |
| Client routing | React Router 7 |
| Client state | TanStack Query 5 |
| Styling | Tailwind CSS 4, CSS custom properties (Caliber navy/teal tokens) |
| UI primitives | Radix UI (rethemed; not default shadcn look) |
| Icons | lucide-react |
| Type generation | openapi-typescript |

---

## Project Structure

Solution layout under `D:\Hackathon Aug26\Caliber\`:

```
Caliber/
├── Caliber.sln
├── README.md
├── specs/
│   ├── solution.md          # Technical design (authoritative)
│   └── blueprint.md         # This document
├── src/
│   └── Caliber.Api/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Caliber.Api.csproj
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── ReportsController.cs
│       │   ├── DashboardController.cs
│       │   ├── EmployeesController.cs
│       │   ├── CertificationsController.cs
│       │   ├── TrainingProgramsController.cs
│       │   ├── SkillsController.cs
│       │   ├── JobRolesController.cs
│       │   ├── ExpirationsController.cs
│       │   ├── EvidenceController.cs
│       │   └── MeController.cs
│       ├── Data/
│       │   ├── CaliberDbContext.cs
│       │   ├── SeedData.cs
│       │   └── Migrations/
│       │       └── 20260819123239_InitialSchema.cs   # exists
│       ├── Models/
│       │   ├── Entities/          # EF entities
│       │   └── Enums/             # Domain enums
│       ├── Dtos/
│       │   ├── Dashboard/
│       │   ├── Employees/
│       │   ├── Certifications/
│       │   ├── Training/
│       │   ├── Skills/
│       │   ├── Evidence/
│       │   └── Common/
│       ├── Validators/            # FluentValidation
│       ├── Services/
│       │   ├── AuthService.cs
│       │   ├── ReportService.cs
│       │   ├── ReadinessService.cs
│       │   ├── CertificationService.cs
│       │   ├── TrainingService.cs
│       │   ├── SkillService.cs
│       │   ├── RoleRequirementService.cs
│       │   ├── EmployeeService.cs
│       │   └── EvidenceService.cs
│       ├── Storage/
│       │   ├── IEvidenceStorage.cs
│       │   └── LocalFileEvidenceStorage.cs
│       ├── Identity/
│       │   ├── ICurrentUser.cs
│       │   ├── CurrentUser.cs
│       │   └── UserAccount.cs (Domain)
│       │   └── PersonaMiddleware.cs   # cookie auth + admin impersonation
│       ├── Exceptions/
│       │   ├── NotFoundException.cs
│       │   ├── ConflictException.cs
│       │   └── ForbiddenException.cs
│       ├── Middleware/
│       │   └── GlobalExceptionHandler.cs
│       └── Extensions/
│           ├── ServiceCollectionExtensions.cs
│           └── WebApplicationExtensions.cs
└── web/
    ├── package.json
    ├── vite.config.ts
    ├── index.html
    ├── tailwind.config.ts          # or @theme in index.css (Tailwind v4)
    └── src/
        ├── main.tsx
        ├── index.css               # iOS design tokens
        ├── api/
        │   ├── client.ts           # fetch wrapper + ProblemDetails
        │   ├── auth.ts               # login, register, logout, me
        │   ├── profile.ts            # profile update, avatar
        │   ├── users.ts              # employee/user CRUD
        │   ├── reports.ts            # report queries
        │   ├── persona.ts            # admin impersonation header
        │   └── generated/          # openapi-typescript output
        ├── hooks/
        │   └── useTheme.ts
        ├── layouts/
        │   └── AppShell.tsx
        ├── pages/
        │   ├── DashboardPage.tsx
        │   ├── EmployeeListPage.tsx
        │   ├── EmployeeProfilePage.tsx
        │   ├── CertificationsPage.tsx
        │   ├── TrainingPage.tsx
        │   ├── SkillsPage.tsx
        │   ├── RolesPage.tsx
        │   ├── ExpirationsPage.tsx
        │   └── MyRequirementsPage.tsx
        ├── components/
        │   ├── ios/                # InsetGroupedList, Row, SegmentedControl, Sheet, etc.
        │   ├── StatusChip.tsx
        │   ├── ReadinessBar.tsx
        │   ├── KpiTile.tsx
        │   ├── EvidenceUploader.tsx
        │   └── ErrorFallback.tsx
        │   ├── contexts/
        │   │   ├── AuthContext.tsx
        │   │   └── PersonaContext.tsx
        │   ├── pages/
        │   │   ├── LoginPage.tsx
        │   │   ├── SignupPage.tsx
        │   │   ├── UsersPage.tsx
        │   │   ├── ProfilePage.tsx
        │   │   ├── ReportsPage.tsx
        │   │   ├── AboutPage.tsx
        │   │   └── … (dashboard, employees, catalogues, etc.)
        │   ├── components/reports/ReportViewer.tsx
        │   ├── styles/reports.css
        └── routes.tsx
```

### Program.cs (target bootstrap)

Replace the default scaffold with cross-cutting registration. Keep `Program.cs` thin — move wiring to extension methods.

```csharp
using Caliber.Api.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/caliber-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddCaliberServices(builder.Configuration);
builder.Services.AddCaliberDb(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(/* XML comments if enabled */);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseCors(CaliberCors.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UsePersonaMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

await app.MigrateAndSeedAsync();

app.Run();
```

---

## Identity Seam (Security/)

Authentication uses **ASP.NET Core cookie auth** with `UserAccount` (email + BCrypt hash). Authorisation is **real**. No controller or service may read `X-Persona-Id` directly — only `ICurrentUser`.

### Auth flow

1. `POST /api/auth/login` or `register` → `SignInAsync` with claims (`EmployeeId`, `AccessLevel`, `LocationId`, `DisplayName`).
2. `UseAuthentication()` validates cookie on each request.
3. `PersonaMiddleware` resolves `ICurrentUser` from claims; admins may override via `X-Persona-Id` impersonation header.
4. Services call `currentUser.EnsureCanAccessEmployee()` before data access.

### ICurrentUser.cs

```csharp
namespace Caliber.Api.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int EmployeeId { get; }
    string DisplayName { get; }
    AccessLevel AccessLevel { get; }
    int LocationId { get; }
    void EnsureCanAccessEmployee(int employeeId, int employeeLocationId);
}
```

### PersonaMiddleware.cs (summary)

- **Public routes:** `/health`, `/api/auth/login`, `/api/auth/register`, `GET /api/locations`, `GET /api/job-roles` (list/detail).
- **Protected routes:** require authenticated cookie; populate `CurrentUser`.
- **Admin impersonation:** if session is Admin and `X-Persona-Id` header present, resolve that employee instead.

**Rule:** `ICurrentUser` is the only identity seam. Middleware is the only place headers and claims are read.

### Logout (frontend)

`useLogout` sets `['auth', 'me']` to `null` in `onMutate` before the API call completes, then removes auth queries on success/error. `AppShell` navigates to `/login` with `replace: true` so the login page appears without refresh.

### FormSection.tsx

Use `FormSection` (not `InsetGroupedList`) when embedding form fields — grouped card styling without `overflow-hidden` clipping.

---

## Entity Layer (Models/Entities/)

Entities mirror the `InitialSchema` migration. Use `[Timestamp] byte[] RowVersion` on mutable assignment entities.

### Enums (Models/Enums/)

```csharp
public enum PersonaKind : byte { Technician = 0, Manager = 1, Admin = 2 }
public enum RequirementKind : byte { Certification = 0, Training = 1, Skill = 2 }
public enum AssignmentStatus : byte { NotStarted = 0, InProgress = 1, Completed = 2, Waived = 3 }
public enum AssignmentSource : byte { RoleTemplate = 0, Direct = 1 }
public enum RequirementStatus : byte
{
    Missing = 0, InProgress = 1, Overdue = 2, Compliant = 3,
    ExpiringSoon = 4, Expired = 5, Waived = 6
}
public enum ProficiencyLevel : byte { Beginner = 0, Intermediate = 1, Advanced = 2, Expert = 3 }
public enum SkillSourceType : byte { Certification = 0, Training = 1, Experience = 2, ManagerAssessed = 3 }
public enum EvidenceType : byte { Certificate = 0, Acknowledgement = 1, Scan = 2, Photo = 3, Other = 4 }
```

### Employee.cs (representative)

```csharp
namespace Caliber.Api.Models.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? ExternalEmployeeNo { get; set; }
    public int JobRoleId { get; set; }
    public int LocationId { get; set; }
    public DateOnly HireDate { get; set; }
    public PersonaKind PersonaKind { get; set; }
    public bool IsActive { get; set; } = true;

    public JobRole JobRole { get; set; } = null!;
    public Location Location { get; set; } = null!;
    public ICollection<EmployeeCertification> Certifications { get; set; } = [];
    public ICollection<EmployeeTraining> Trainings { get; set; } = [];
    public ICollection<EmployeeSkill> Skills { get; set; } = [];
}
```

Implement all entities listed in solution.md: `Location`, `Department`, `JobRole`, `Certification`, `EmployeeCertification`, `CertificationAward`, `TrainingProgram`, `TrainingModule`, `EmployeeTraining`, `Skill`, `CertificationSkill`, `TrainingProgramSkill`, `EmployeeSkill`, `RoleRequirement`, `Evidence`.

---

## DbContext (Data/CaliberDbContext.cs)

```csharp
namespace Caliber.Api.Data;

public class CaliberDbContext(DbContextOptions<CaliberDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();
    public DbSet<CertificationAward> CertificationAwards => Set<CertificationAward>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    // ... remaining DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CaliberDbContext).Assembly);

        // Unique constraints
        modelBuilder.Entity<EmployeeCertification>()
            .HasIndex(x => new { x.EmployeeId, x.CertificationId }).IsUnique();
        modelBuilder.Entity<EmployeeTraining>()
            .HasIndex(x => new { x.EmployeeId, x.TrainingProgramId }).IsUnique();
        modelBuilder.Entity<EmployeeSkill>()
            .HasIndex(x => new { x.EmployeeId, x.SkillId }).IsUnique();

        // Performance indexes (match migration)
        modelBuilder.Entity<EmployeeCertification>()
            .HasIndex(x => new { x.EmployeeId, x.Status });
        modelBuilder.Entity<CertificationAward>()
            .HasIndex(x => new { x.EmployeeCertificationId, x.AwardedOn })
            .IsDescending(false, true);

        // RoleRequirement: exactly one FK matches RequirementKind — enforce in Fluent API or check constraint
    }
}
```

**Key settings:**

- `AddDbContext` with SQL Server connection string from configuration.
- `AddHealthChecks().AddDbContextCheck<CaliberDbContext>()`.
- Do **not** enable `UseQueryTrackingBehavior` globally; use `AsNoTracking()` explicitly on reads.

### SeedData.cs

Run on startup when `Employees` is empty:

```csharp
public static class SeedData
{
    public static async Task EnsureSeededAsync(CaliberDbContext db)
    {
        if (await db.Employees.AnyAsync()) return;

        // 3 locations, 4 departments, 5 job roles, ~12 employees
        // Equipment-industry certifications, training programs, skills
        // Role requirement templates
        // Staged statuses: compliant, expiring soon, expired, new hire with empty checklist

        await db.SaveChangesAsync();
    }
}
```

---

## ReadinessService (Services/ReadinessService.cs)

**Single source of truth for computed status.** No controller, query, or React component may derive status independently.

### RequirementStatusDto (unified projection)

```csharp
public sealed record RequirementStatusDto
{
    public RequirementKind Kind { get; init; }
    public int SourceId { get; init; }              // EmployeeCertificationId or EmployeeTrainingId
    public int CatalogueId { get; init; }
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public AssignmentStatus AssignmentStatus { get; init; }
    public DateOnly? CompletedOn { get; init; }
    public DateOnly? EffectiveDate { get; init; }   // ExpiresOn or NextDueOn
    public DateOnly? DueOn { get; init; }
    public int WarningDays { get; init; }
    public RequirementStatus Status { get; init; }
    public bool IsMandatory { get; init; } = true;
}
```

### Status computation (pure function)

```csharp
public static RequirementStatus ComputeStatus(
    AssignmentStatus assignmentStatus,
    bool isCompleted,
    DateOnly? effectiveDate,
    DateOnly? dueOn,
    int warningDays,
    DateOnly today)
{
    if (assignmentStatus == AssignmentStatus.Waived)
        return RequirementStatus.Waived;

    if (isCompleted && effectiveDate is not null && effectiveDate < today)
        return RequirementStatus.Expired;

    if (isCompleted && effectiveDate is not null
        && effectiveDate <= today.AddDays(warningDays))
        return RequirementStatus.ExpiringSoon;

    if (isCompleted)
        return RequirementStatus.Compliant;

    if (!isCompleted && dueOn is not null && dueOn < today)
        return RequirementStatus.Overdue;

    if (assignmentStatus == AssignmentStatus.InProgress)
        return RequirementStatus.InProgress;

    return RequirementStatus.Missing;
}
```

Order is intentional — see solution.md.

### Latest award resolution (EF projection)

Use a grouped subquery or window function pattern. Example for certification side:

```csharp
var latestAwards = db.CertificationAwards
    .GroupBy(a => a.EmployeeCertificationId)
    .Select(g => new
    {
        EmployeeCertificationId = g.Key,
        Latest = g.OrderByDescending(a => a.AwardedOn).First()
    });

var certRequirements = from ec in db.EmployeeCertifications.AsNoTracking()
                       join c in db.Certifications on ec.CertificationId equals c.Id
                       join la in latestAwards on ec.Id equals la.EmployeeCertificationId into awards
                       from la in awards.DefaultIfEmpty()
                       where ec.EmployeeId == employeeId
                       select new RequirementStatusDto
                       {
                           Kind = RequirementKind.Certification,
                           SourceId = ec.Id,
                           CatalogueId = c.Id,
                           Name = c.Name,
                           AssignmentStatus = ec.Status,
                           CompletedOn = la != null ? la.Latest.AwardedOn : null,
                           EffectiveDate = la != null ? la.Latest.ExpiresOn : null,
                           DueOn = ec.DueOn,
                           WarningDays = c.ExpiryWarningDays,
                           // Status computed in memory after materialisation, or via client-evaluated helper
                       };
```

Union with training side, then apply `ComputeStatus` per row.

### Readiness percentage

```csharp
public static decimal ComputeReadinessPercent(IEnumerable<RequirementStatusDto> requirements)
{
    var mandatory = requirements.Where(r => r.IsMandatory).ToList();
    if (mandatory.Count == 0) return 100m;
    var compliant = mandatory.Count(r => r.Status == RequirementStatus.Compliant
                                      || r.Status == RequirementStatus.ExpiringSoon);
    return Math.Round(100m * compliant / mandatory.Count, 1);
}
```

`ExpiringSoon` counts as compliant for the percentage — still valid, just needs renewal planning.

---

## Service Layer

| Service | Responsibility |
| --- | --- |
| `ReadinessService` | Unified projections, dashboard aggregates, expirations buckets, readiness rollups |
| `EmployeeService` | Employee list/detail with scoping; profile assembly |
| `CertificationService` | Catalogue CRUD; assign; record award; waive; skill granting on award |
| `TrainingService` | Catalogue CRUD; assign; progress update; complete; acknowledge; skill granting |
| `SkillService` | Catalogue CRUD; manual assign/assess |
| `RoleRequirementService` | Template CRUD; apply-to-role (idempotent) |
| `EvidenceService` | Upload validation, metadata persistence, verify, delete, authorised download |

### Skill granting (shared helper)

Called inside the same transaction as award/complete:

```csharp
internal static async Task GrantSkillsFromCertificationAsync(
    CaliberDbContext db,
    int employeeId,
    int certificationId,
    DateOnly assessedOn,
    string assessedBy,
    CancellationToken ct)
{
    var grants = await db.CertificationSkills
        .AsNoTracking()
        .Where(x => x.CertificationId == certificationId)
        .ToListAsync(ct);

    foreach (var grant in grants)
    {
        var existing = await db.EmployeeSkills
            .SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.SkillId == grant.SkillId, ct);

        if (existing is null)
        {
            db.EmployeeSkills.Add(new EmployeeSkill
            {
                EmployeeId = employeeId,
                SkillId = grant.SkillId,
                ProficiencyLevel = grant.GrantedProficiency,
                SourceType = SkillSourceType.Certification,
                SourceCertificationId = certificationId,
                AssessedOn = assessedOn,
                AssessedBy = assessedBy
            });
        }
        else if (existing.ProficiencyLevel < grant.GrantedProficiency)
        {
            existing.ProficiencyLevel = grant.GrantedProficiency;
            existing.SourceType = SkillSourceType.Certification;
            existing.SourceCertificationId = certificationId;
            existing.AssessedOn = assessedOn;
            existing.AssessedBy = assessedBy;
        }
        // Equal or higher proficiency: leave unchanged (manager assessment preserved)
    }
}
```

Mirror for `GrantSkillsFromTrainingAsync`.

### Apply-to-role (idempotent)

```csharp
public async Task ApplyRoleRequirementsAsync(int jobRoleId, CancellationToken ct)
{
    var role = await db.JobRoles
        .Include(r => r.Requirements)
        .Include(r => r.Employees.Where(e => e.IsActive))
        .SingleOrDefaultAsync(r => r.Id == jobRoleId, ct)
        ?? throw new NotFoundException("JobRole", jobRoleId);

    foreach (var employee in role.Employees)
    {
        foreach (var req in role.Requirements)
        {
            switch (req.RequirementKind)
            {
                case RequirementKind.Certification when req.CertificationId is not null:
                    if (!await db.EmployeeCertifications.AnyAsync(
                        x => x.EmployeeId == employee.Id && x.CertificationId == req.CertificationId, ct))
                    {
                        db.EmployeeCertifications.Add(new EmployeeCertification
                        {
                            EmployeeId = employee.Id,
                            CertificationId = req.CertificationId.Value,
                            Status = AssignmentStatus.NotStarted,
                            Source = AssignmentSource.RoleTemplate,
                            AssignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                            DueOn = req.DueWithinDaysOfHire is int days
                                ? employee.HireDate.AddDays(days)
                                : null
                        });
                    }
                    break;
                // Training and Skill cases analogous
            }
        }
    }

    await db.SaveChangesAsync(ct);
}
```

---

## Controllers (Controllers/)

Thin controllers — delegate to services, return DTOs, no business logic.

```csharp
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(EmployeeService employees, ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> List(
        [FromQuery] EmployeeListQuery query, CancellationToken ct)
        => Ok(await employees.ListAsync(query, user, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeProfileDto>> Get(int id, CancellationToken ct)
        => Ok(await employees.GetProfileAsync(id, user, ct));

    [HttpGet("{id:int}/requirements")]
    public async Task<ActionResult<IReadOnlyList<RequirementStatusDto>>> Requirements(
        int id, CancellationToken ct)
        => Ok(await employees.GetRequirementsAsync(id, user, ct));
}
```

Register FluentValidation validators; invalid DTOs return 400 ProblemDetails with field errors.

---

## Exception Handling (Middleware/GlobalExceptionHandler.cs)

Map domain exceptions to RFC 9457 ProblemDetails:

| Exception | Status |
| --- | --- |
| `NotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 400 with `errors` extension |
| `ConflictException` | 409 |
| `ForbiddenException` | 403 |
| `UnsupportedMediaTypeException` | 415 |
| `DbUpdateConcurrencyException` | 409 "Reload and retry" |
| Unhandled | 500 with `traceId` only |

Never leak stack traces outside Development.

---

## Evidence Storage (Storage/ package)

### IEvidenceStorage.cs

```csharp
public interface IEvidenceStorage
{
    Task<StoredEvidenceFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken ct);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct);
    Task DeleteAsync(string storedFileName, CancellationToken ct);
}

public sealed record StoredEvidenceFile(string StoredFileName, long SizeBytes);
```

### LocalFileEvidenceStorage.cs

- Root path: `App_Data/evidence/` (outside `wwwroot`, gitignored).
- Filename: `{guid}{safeExtension}` — never use the original name for paths.
- Allowlist: `.pdf`, `.png`, `.jpg`, `.jpeg`, `.webp`.
- Validate declared MIME **and** magic bytes.
- Max size: configurable (e.g. 10 MB), enforced server-side.

`EvidenceController` is the only public access path for file bytes.

---

## Front-End Implementation (web/)

### vite.config.ts (target)

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'https://localhost:7xxx', changeOrigin: true, secure: false },
      '/health': { target: 'https://localhost:7xxx', changeOrigin: true, secure: false },
    },
  },
})
```

### API client (api/client.ts)

```typescript
export class ApiError extends Error {
  constructor(
    public status: number,
    public title: string,
    public traceId?: string,
    public fieldErrors?: Record<string, string[]>,
  ) {
    super(title)
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(getPersonaId() ? { 'X-Persona-Id': getPersonaId() } : {}),
      ...init?.headers,
    },
  })

  if (!res.ok) {
    const problem = await res.json().catch(() => ({}))
    throw new ApiError(
      res.status,
      problem.title ?? res.statusText,
      problem.traceId,
      problem.errors,
    )
  }

  return res.status === 204 ? (undefined as T) : res.json()
}
```

### TanStack Query defaults (main.tsx)

```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
})
```

Mutations use optimistic updates with rollback on `ApiError`.

### Design tokens (index.css)

Define CSS variables on `:root` and `.dark` per solution.md (iOS system palette). Tailwind `@theme` maps `--color-accent`, `--color-grouped-bg`, `--color-separator`, status colours, etc. No component uses literal hex values.

### Route map

| Path | Component | Persona |
| --- | --- | --- |
| `/` | `DashboardPage` | Manager, Admin |
| `/employees` | `EmployeeListPage` | Manager, Admin |
| `/employees/:id` | `EmployeeProfilePage` | Manager, Admin |
| `/certifications` | `CertificationsPage` | Manager, Admin |
| `/training` | `TrainingPage` | Manager, Admin |
| `/skills` | `SkillsPage` | Manager, Admin |
| `/roles` | `RolesPage` | Admin |
| `/expirations` | `ExpirationsPage` | Manager, Admin |
| `/my` | `MyRequirementsPage` | Technician (default landing) |

`AppShell` renders sidebar nav; technicians see only `/my`. Persona switcher and theme toggle pinned at sidebar bottom.

### openapi-typescript

Add script to `package.json`:

```json
"generate:api": "openapi-typescript https://localhost:7xxx/swagger/v1/swagger.json -o src/api/generated/schema.d.ts"
```

Run after API is up with Swagger enabled.

---

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Caliber": "Server=(localdb)\\MSSQLLocalDB;Database=Caliber;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "EvidenceStorage": {
    "RootPath": "App_Data/evidence",
    "MaxBytes": 10485760,
    "AllowedExtensions": [ ".pdf", ".png", ".jpg", ".jpeg", ".webp" ]
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

`appsettings.Development.json` may override the connection string for a full SQL Server instance (e.g. `Server=localhost;Database=Caliber;...`). Do not commit machine-specific secrets.

---

## RequirementStatus Lifecycle

```
NotStarted / InProgress
        │
        ├──→ Waived (explicit manager action)
        │
        ├──→ Overdue (DueOn passed, not complete)
        │
        └──→ Completed
                 │
                 ├──→ Compliant (no expiry, or expiry beyond warning window)
                 ├──→ ExpiringSoon (EffectiveDate within WarningDays)
                 └──→ Expired (EffectiveDate passed)
```

- **Certification `EffectiveDate`**: latest `CertificationAward.ExpiresOn`.
- **Training `EffectiveDate`**: `EmployeeTraining.NextDueOn` (from `CompletedOn + RecurrenceMonths`; null if one-time).
- Status is **computed at read time**, never stored.

---

## EF Query Patterns Reference

Consolidated patterns used across services. Prefer one query per screen; no N+1.

### Q1: Dashboard KPIs (ReadinessService.GetDashboardAsync)

Single query grouping mandatory requirements by computed status across scoped employees. Build certification and training projections as subqueries, union, group in SQL or materialise once and aggregate in memory for demo scale.

Target output:

```csharp
public sealed record DashboardDto(
    decimal OverallCompliancePercent,
    int EmployeesFullyReady,
    int ExpiringWithin60Days,
    int ExpiredOrOverdue,
    IReadOnlyList<ExpiringItemDto> ExpiringSoonFeed,
    IReadOnlyList<LocationComplianceDto> ByLocation,
    IReadOnlyList<GapItemDto> TopGaps);
```

### Q2: Employee list with readiness (EmployeeService.ListAsync)

```csharp
var baseQuery = db.Employees.AsNoTracking().Where(e => e.IsActive);

if (user.IsManager)
    baseQuery = baseQuery.Where(e => e.LocationId == user.LocationId);

var employees = await baseQuery
    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
    .Skip(query.Offset).Take(query.Limit)
    .Select(e => new EmployeeListItemDto
    {
        Id = e.Id,
        FullName = e.FirstName + " " + e.LastName,
        JobRole = e.JobRole.Name,
        Location = e.Location.Name,
        // ReadinessPercent filled by ReadinessService batch call for page IDs
    })
    .ToListAsync(ct);
```

Batch-load requirements for the page's employee IDs in one additional query rather than per-row.

### Q3: Expirations buckets (ReadinessService.GetExpirationsAsync)

Filter unified projection where `Status == ExpiringSoon` or `EffectiveDate` within 30/60/90 day windows from today. Order by `EffectiveDate`.

### Q4: Scoping guard (every employee-scoped read)

```csharp
user.EnsureCanAccessEmployee(employee.Id, employee.LocationId);
// Manager: employee.LocationId must equal user.LocationId
// Technician: employee.Id must equal user.EmployeeId
// Admin: always allowed
```

Apply **before** returning data, inside the service method, not in the controller after fetch.

---

## Implementation Milestones

Execute in order. Each milestone should leave `main` runnable.

| # | Milestone | Deliverable |
| --- | --- | --- |
| 1 | **Entities + DbContext** | Model classes matching migration; `CaliberDbContext`; DI registration |
| 2 | **Cross-cutting** | Serilog, ProblemDetails handler, CORS, security headers, rate limiter, persona middleware, health check |
| 3 | **Seed data** | `SeedData` with demo-ready statuses; auto-run on empty DB |
| 4 | **ReadinessService** | Unified projection, status computation, dashboard + expirations queries |
| 5 | **Read API** | Dashboard, employees, catalogues, `/api/me/requirements` |
| 6 | **Write API** | Assign, award, complete, waive, apply-to-role, skill granting |
| 7 | **Evidence** | Upload, download, verify, storage hardening |
| 8 | **Design system** | iOS tokens, `InsetGroupedList`, `StatusChip`, `AppShell`, theme toggle |
| 9 | **Screens** | Dashboard, employee list/profile, catalogues, roles, expirations, `/my` |
| 10 | **Polish + verify** | NFR checklist from solution.md; README with clone-to-run and demo script |

**Cut from scope (do not implement):** readiness matrix heatmap, skill-based talent finder, module-level training progress UI.

---

## Non-Functional Checklist (pre-demo)

From solution.md — run manually before demo:

| Quality | Check |
| --- | --- |
| Responsiveness | 390 / 768 / 1280 / 1920px; no horizontal scroll |
| Exception handling | Force 500 → ProblemDetails + friendly error card, no stack trace |
| Security | Technician cannot `GET /api/employees/{otherId}` (403); renamed `.exe` upload rejected |
| Speed | Dashboard cold < 1.5s; EF log shows no N+1 on dashboard/profile |
| No lag | Route transitions feel instant; optimistic mutations; skeleton loaders |

---

## Reference Documents

- [solution.md](./solution.md) — authoritative technical design
- Certification, Training & Skills Evidence Tracker — original requirements brief
- [RFC 9457 - Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457)
- [Apple Human Interface Guidelines - Color](https://developer.apple.com/design/human-interface-guidelines/color)
- [TanStack Query docs](https://tanstack.com/query/latest)
- [openapi-typescript](https://openapi-ts.dev/)
