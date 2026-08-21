# PRD: Caliber — Workforce Readiness Web Application

## Project Overview

Implement **Caliber** — a standalone workforce readiness web application for equipment dealerships. The system tracks employee **certifications**, **training**, **skills**, and supporting **evidence**, and presents a unified **readiness** view so managers can see compliance gaps, upcoming expirations, and capability at a glance.

The solution consists of:

- **`Caliber.Api`** — ASP.NET Core 8 Web API with EF Core 8 and SQL Server (`Caliber` database, default `(localdb)\MSSQLLocalDB`).
- **`web`** — React 19 + TypeScript SPA (Vite 8, TanStack Query, Tailwind CSS 4, iOS-inspired light/dark design).

The organising principle is **separate write models, one unified read projection**: certifications and training are distinct aggregates; `ReadinessService` projects both into `RequirementStatusDto` so dashboard, employee list, profile, and expirations screens are written once.

Authentication uses **cookie-based login** (email + password, BCrypt) with signup, change-password, and an admin-only impersonation header (`X-Persona-Id`). **Authorisation is real** — managers are scoped to their location, technicians to themselves, enforced inside queries via `ICurrentUser`.

**Implemented (2026-08-20 enhancement + final polish):** Full auth, navy/teal rebrand, user management, profile/avatars, catalogue edit/deactivate, skill grants UI, reporting module (**7 reports**, print PDF), About page, evidence upload fix, dashboard analytics charts, sidebar color presets, skills catalogue edit/deactivate, auth navigation hardening.

**Cut from scope (do not implement):** readiness matrix heatmap, skill-based talent finder, module-level training progress UI, **email/SMS/external push** notifications, server-side PDF generation.

**Added in round 3 (2026-08-21):** In-app notification feed (bell UI), manager broadcast announcements, dashboard **Notify** actions, renewal request/approve/decline workflow (Accept/Reject in notification panel), granted-skills editor on cert/training create + detail, acknowledge-once with manager notification, waive helper text, login redirect fix, report headers (org name + title), fully-ready KPI aligned with compliance leaders report.

Primary references: [solution.md](./solution.md) (design authority), [blueprint.md](./blueprint.md) (implementation guide).

## Created

- **Date:** 2026-08-19
- **Author:** Nafay Ali

---

## Tasks

### PHASE 1: API CROSS-CUTTING & CONFIGURATION

#### Task 1: Wire Program.cs and extension methods
- **ID:** CAL-001
- **Status:** [x] Complete
- **Description:** Replace the default scaffold `Program.cs` with Serilog, service registration via extension methods, middleware pipeline, Swagger (Development), health checks, and startup migration + seed hook. Keep `Program.cs` thin.
- **File:** `src/Caliber.Api/Program.cs`, `src/Caliber.Api/Extensions/ServiceCollectionExtensions.cs`, `src/Caliber.Api/Extensions/WebApplicationExtensions.cs`
- **Acceptance Criteria:**
    - Serilog writes to console and rolling file under `logs/caliber-.log`
    - `AddCaliberServices` registers all services, validators, `ICurrentUserAccessor`, `IEvidenceStorage`
    - `AddCaliberDb` registers `CaliberDbContext` with SQL Server connection string
    - Pipeline order: Serilog request logging → global exception handler → HTTPS → security headers → CORS → rate limiter → persona middleware → authorization
    - `/health` includes database check
    - `MigrateAndSeedAsync` applies pending migrations and calls `SeedData.EnsureSeededAsync` when DB is empty
    - Solution builds with `dotnet build`

#### Task 2: Create appsettings configuration
- **ID:** CAL-002
- **Status:** [x] Complete
- **Description:** Add committed configuration for connection string (LocalDB default), evidence storage, CORS allowed origins, and Serilog levels. Development overrides in `appsettings.Development.json`.
- **File:** `src/Caliber.Api/appsettings.json`, `src/Caliber.Api/appsettings.Development.json`
- **Acceptance Criteria:**
    - `ConnectionStrings:Caliber` defaults to `(localdb)\MSSQLLocalDB;Database=Caliber;Trusted_Connection=True;TrustServerCertificate=True`
    - `EvidenceStorage:RootPath`, `MaxBytes` (10485760), `AllowedExtensions` configured
    - `Cors:AllowedOrigins` includes `http://localhost:5173`
    - EF Core log level Warning in Production; no secrets committed

#### Task 3: Global exception handler and domain exceptions
- **ID:** CAL-003
- **Status:** [x] Complete
- **Description:** Implement RFC 9457 `ProblemDetails` global handler and domain exceptions: `NotFoundException`, `ConflictException`, `ForbiddenException`. Map FluentValidation failures, concurrency conflicts, and unhandled errors.
- **File:** `src/Caliber.Api/Middleware/GlobalExceptionHandler.cs`, `src/Caliber.Api/Exceptions/*.cs`
- **Acceptance Criteria:**
    - 404 for `NotFoundException`, 409 for `ConflictException` and `DbUpdateConcurrencyException`, 403 for `ForbiddenException`, 400 with field `errors` for validation, 500 with `traceId` only for unhandled
    - Stack traces never returned outside Development
    - Every error response includes a correlatable `traceId`

#### Task 4: Security headers, CORS, and rate limiting
- **ID:** CAL-004
- **Status:** [x] Complete
- **Description:** Add security headers middleware (CSP, `X-Content-Type-Options`, `Referrer-Policy`, frame-ancestors denial), CORS policy locked to configured Vite origin, and rate limiting on upload and persona routes.
- **File:** `src/Caliber.Api/Extensions/WebApplicationExtensions.cs` or dedicated middleware
- **Acceptance Criteria:**
    - CORS never uses wildcard with credentials
    - Rate limiter returns 429 with `Retry-After` when exceeded
    - Security headers present on API responses (verifiable via curl)

---

### PHASE 2: DOMAIN ENUMS & ENTITY LAYER

#### Task 5: Create domain enums
- **ID:** CAL-005
- **Status:** [x] Complete
- **Description:** Create all domain enums used across entities, DTOs, and services.
- **File:** `src/Caliber.Api/Models/Enums/*.cs`
- **Acceptance Criteria:**
    - Enums defined: `PersonaKind`, `RequirementKind`, `AssignmentStatus`, `AssignmentSource`, `RequirementStatus`, `ProficiencyLevel`, `SkillSourceType`, `EvidenceType`, plus catalogue category enums for certification/training/skill
    - Stored as `byte`/`int` matching migration column types

#### Task 6: Create organisation entities
- **ID:** CAL-006
- **Status:** [x] Complete
- **Description:** Create EF entities: `Location`, `Department`, `JobRole`, `Employee` with navigation properties and constraints matching `InitialSchema`.
- **File:** `src/Caliber.Api/Models/Entities/Location.cs`, `Department.cs`, `JobRole.cs`, `Employee.cs`
- **Acceptance Criteria:**
    - `Employee.ExternalEmployeeNo` nullable (Aspen integration seam)
    - `Employee.PersonaKind` drives persona switcher
    - Unique index on `Employee.Email` reflected in Fluent configuration
    - Navigation: Employee → JobRole, Location; JobRole → Department

#### Task 7: Create certification aggregate entities
- **ID:** CAL-007
- **Status:** [x] Complete
- **Description:** Create `Certification`, `EmployeeCertification`, `CertificationAward`, `CertificationSkill` entities.
- **File:** `src/Caliber.Api/Models/Entities/Certification*.cs`
- **Acceptance Criteria:**
    - `EmployeeCertification` has `[Timestamp] RowVersion`, audit columns, unique `(EmployeeId, CertificationId)`
    - `CertificationAward` is append-only renewal history with `AwardedOn`, computed `ExpiresOn`
    - `CertificationSkill` composite key on `(CertificationId, SkillId)` with `GrantedProficiency`

#### Task 8: Create training aggregate entities
- **ID:** CAL-008
- **Status:** [x] Complete
- **Description:** Create `TrainingProgram`, `TrainingModule`, `EmployeeTraining`, `TrainingProgramSkill` entities.
- **File:** `src/Caliber.Api/Models/Entities/Training*.cs`
- **Acceptance Criteria:**
    - `EmployeeTraining` has `PercentComplete`, `AcknowledgedOn`/`AcknowledgedBy`, `NextDueOn`, `RowVersion`, audit columns
    - `TrainingModule` retained in schema (no UI in this release)
    - Unique `(EmployeeId, TrainingProgramId)`

#### Task 9: Create skills, requirements, and evidence entities
- **ID:** CAL-009
- **Status:** [x] Complete
- **Description:** Create `Skill`, `EmployeeSkill`, `RoleRequirement`, `Evidence` entities.
- **File:** `src/Caliber.Api/Models/Entities/Skill.cs`, `EmployeeSkill.cs`, `RoleRequirement.cs`, `Evidence.cs`
- **Acceptance Criteria:**
    - `EmployeeSkill` unique `(EmployeeId, SkillId)`; optional `SourceCertificationId` / `SourceTrainingProgramId`
    - `RoleRequirement` has exactly one target FK matching `RequirementKind` (enforced in Fluent API or check constraint)
    - `Evidence` stores metadata only; file bytes on disk via `IEvidenceStorage`

#### Task 10: Create CaliberDbContext
- **ID:** CAL-010
- **Status:** [x] Complete
- **Description:** Implement `CaliberDbContext` with all `DbSet`s, relationship configuration, indexes matching migration, and optional `IEntityTypeConfiguration` classes.
- **File:** `src/Caliber.Api/Data/CaliberDbContext.cs`, `src/Caliber.Api/Data/Configurations/*.cs`
- **Acceptance Criteria:**
    - All tables from `InitialSchema` mapped; no pending model drift vs migration snapshot
    - Performance indexes: `EmployeeCertification(EmployeeId, Status)`, `CertificationAward(EmployeeCertificationId, AwardedOn DESC)`, `EmployeeTraining(NextDueOn)`, etc.
    - `dotnet ef database update` succeeds on clean LocalDB

---

### PHASE 3: IDENTITY & AUTHORISATION

#### Task 11: Implement ICurrentUser and PersonaMiddleware
- **ID:** CAL-011
- **Status:** [x] Complete
- **Description:** Create `ICurrentUser`, `CurrentUser`, `ICurrentUserAccessor`, and `PersonaMiddleware` resolving caller from `X-Persona-Id` header. **No controller or service may read the header directly.**
- **File:** `src/Caliber.Api/Identity/ICurrentUser.cs`, `CurrentUser.cs`, `ICurrentUserAccessor.cs`, `PersonaMiddleware.cs`
- **Acceptance Criteria:**
    - Missing or invalid header returns 401
    - Manager: `LocationId` set; Admin: `LocationId` null (all locations); Technician: self only
    - `EnsureCanAccessEmployee(employeeId, employeeLocationId)` throws `ForbiddenException` on IDOR attempt
    - Middleware registered before authorization in pipeline

---

### PHASE 4: SEED DATA

#### Task 12: Implement SeedData
- **ID:** CAL-012
- **Status:** [x] Complete
- **Description:** Populate demo data when `Employees` table is empty: 3 locations, 4 departments, 5 job roles, ~12 employees, equipment-industry certifications/training/skills, role requirement templates, staged statuses (compliant, expiring soon, expired, new hire empty checklist).
- **File:** `src/Caliber.Api/Data/SeedData.cs`
- **Acceptance Criteria:**
    - Idempotent: skips if any employee exists
    - At least one manager persona and one technician persona for demo switching
    - John Deere / Kubota / OSHA / EPA-style seed content per solution.md
    - Fresh clone + startup yields demo-ready dashboard data

---

### PHASE 5: READINESS ENGINE

#### Task 13: Implement ReadinessService core
- **ID:** CAL-013
- **Status:** [x] Complete
- **Description:** Implement `ReadinessService` as the **single source of truth** for computed status. Include `RequirementStatusDto`, static `ComputeStatus` (ordered rules from solution.md), unified certification + training projection, latest-award resolution, and `ComputeReadinessPercent`.
- **File:** `src/Caliber.Api/Services/ReadinessService.cs`, `src/Caliber.Api/Dtos/Common/RequirementStatusDto.cs`
- **Acceptance Criteria:**
    - Status evaluation order: Waived → Expired → ExpiringSoon → Compliant → Overdue → InProgress → Missing
    - Status is never persisted to database
    - `ExpiringSoon` counts toward readiness percentage
    - Latest `CertificationAward` resolved per assignment (no N+1 per employee on list pages — batch where possible)
    - No other class computes `RequirementStatus`

#### Task 14: Dashboard and expirations queries
- **ID:** CAL-014
- **Status:** [x] Complete
- **Description:** Implement `GetDashboardAsync` and `GetExpirationsAsync` returning KPI tiles, expiring-soon feed, compliance by location, top gaps, and 30/60/90 day buckets.
- **File:** `src/Caliber.Api/Services/ReadinessService.cs`, `src/Caliber.Api/Dtos/Dashboard/*.cs`
- **Acceptance Criteria:**
    - Dashboard obeys manager location scoping via `ICurrentUser`
    - Single round trip for dashboard (or minimal bounded queries — no per-employee loops in C#)
    - `DashboardDto` includes: overall compliance %, employees fully ready, expiring within 60 days, expired/overdue count, feeds/lists
    - All read queries use `AsNoTracking()` and project to DTOs

---

### PHASE 6: DTOs & VALIDATION

#### Task 15: Create request/response DTOs
- **ID:** CAL-015
- **Status:** [x] Complete
- **Description:** Create DTO records for all API endpoints: dashboard, employees, certifications, training, skills, job roles, evidence, paging wrappers.
- **File:** `src/Caliber.Api/Dtos/**/*.cs`
- **Acceptance Criteria:**
    - DTOs expose only fields needed by UI (no internal entity leakage)
    - Paged list wrapper: `PagedResult<T>` with `Items`, `TotalCount`, `Offset`, `Limit`
    - OpenAPI/Swagger documents all public endpoints

#### Task 16: FluentValidation validators
- **ID:** CAL-016
- **Status:** [x] Complete
- **Description:** Add validators for all write DTOs: assign certification/training, record award, complete training, waive, assign skill, role requirement, evidence upload metadata.
- **File:** `src/Caliber.Api/Validators/*.cs`
- **Acceptance Criteria:**
    - Validators registered via `AddValidatorsFromAssemblyContaining`
    - Invalid requests return 400 ProblemDetails with per-field `errors`
    - Date rules: `AwardedOn` not in future; `DueOn` optional; proficiency enum valid

---

### PHASE 7: SERVICE LAYER (WRITE PATHS)

#### Task 17: CertificationService
- **ID:** CAL-017
- **Status:** [x] Complete
- **Description:** Catalogue CRUD; assign certification; record award (append `CertificationAward`, compute `ExpiresOn`); waive; trigger skill granting on award.
- **File:** `src/Caliber.Api/Services/CertificationService.cs`
- **Acceptance Criteria:**
    - Duplicate assign returns 409
    - Award appends history (does not overwrite prior awards)
    - Skill granting in same transaction as award; higher proficiency wins; never downgrades manager assessment
    - Authorisation enforced before mutations

#### Task 18: TrainingService
- **ID:** CAL-018
- **Status:** [x] Complete
- **Description:** Catalogue CRUD; assign training; update progress (`PercentComplete`, `StartedOn`); complete (compute `NextDueOn`); acknowledge when `RequiresAcknowledgement`; skill granting on complete.
- **File:** `src/Caliber.Api/Services/TrainingService.cs`
- **Acceptance Criteria:**
    - Complete sets `CompletedOn` and `NextDueOn` from `RecurrenceMonths`
    - Acknowledge records `AcknowledgedOn`/`AcknowledgedBy`
    - Skill granting mirrors certification rules

#### Task 19: SkillService and RoleRequirementService
- **ID:** CAL-019
- **Status:** [x] Complete
- **Description:** Skill catalogue CRUD; manual assign/reassess with proficiency and source. Role template CRUD; **idempotent** apply-to-role generating missing assignments with `DueWithinDaysOfHire`.
- **File:** `src/Caliber.Api/Services/SkillService.cs`, `RoleRequirementService.cs`
- **Acceptance Criteria:**
    - Apply-to-role safe to run repeatedly; never duplicates assignments
    - Role requirements support Certification, Training, and Skill kinds
    - Skill assign records `SourceType` = Experience or ManagerAssessed when manual

#### Task 20: EmployeeService
- **ID:** CAL-020
- **Status:** [x] Complete
- **Description:** Employee list (paged, filtered), profile assembly (requirements, skills, evidence collections), scoping in query.
- **File:** `src/Caliber.Api/Services/EmployeeService.cs`
- **Acceptance Criteria:**
    - Manager sees only own location; technician denied on other employee IDs
    - List includes readiness summary per employee (batch readiness call)
    - Filters: location, job role, status, search text

---

### PHASE 8: EVIDENCE STORAGE & API

#### Task 21: LocalFileEvidenceStorage
- **ID:** CAL-021
- **Status:** [x] Complete
- **Description:** Implement `IEvidenceStorage` writing to `App_Data/evidence/` outside `wwwroot`. GUID filenames, extension + MIME allowlist, magic-byte validation, size cap.
- **File:** `src/Caliber.Api/Storage/IEvidenceStorage.cs`, `LocalFileEvidenceStorage.cs`
- **Acceptance Criteria:**
    - Allowed: PDF, PNG, JPEG, WebP only
    - Renamed executable rejected even with `.pdf` extension
    - Original filename stored for display only; never used in path
    - Directory created on first write; path in `.gitignore`

#### Task 22: EvidenceService and EvidenceController
- **ID:** CAL-022
- **Status:** [x] Complete
- **Description:** Multipart upload linked to certification/training/skill assignment; authorised streaming download; manager verify; delete removes DB row and file.
- **File:** `src/Caliber.Api/Services/EvidenceService.cs`, `Controllers/EvidenceController.cs`
- **Acceptance Criteria:**
    - Technician may upload only for self; manager for location-scoped employees
    - Download returns `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff`
    - Verify sets `IsVerified`, `VerifiedBy`, `VerifiedOn`
    - Oversize upload returns 413; bad type returns 415

---

### PHASE 9: API CONTROLLERS

#### Task 23: Read controllers
- **ID:** CAL-023
- **Status:** [x] Complete
- **Description:** Implement thin controllers: `DashboardController`, `EmployeesController`, `CertificationsController`, `TrainingProgramsController`, `SkillsController`, `JobRolesController`, `ExpirationsController`, `MeController`.
- **File:** `src/Caliber.Api/Controllers/*.cs`
- **Acceptance Criteria:**
    - Routes match solution.md API table (`/api/dashboard`, `/api/employees`, etc.)
    - `GET /api/me/requirements` always scoped to caller (technician self-service)
    - Controllers contain no business logic — delegate to services
    - Swagger lists all endpoints; `dotnet run` serves API

#### Task 24: Write controllers
- **ID:** CAL-024
- **Status:** [x] Complete
- **Description:** Implement write endpoints for assignments, awards, completions, waivers, acknowledgements, apply-to-role, skill assign, evidence CRUD.
- **File:** `src/Caliber.Api/Controllers/*.cs` (action methods on existing controllers)
- **Acceptance Criteria:**
    - Optimistic concurrency: stale `RowVersion` returns 409 with actionable message
    - All write paths call authorisation before DB access
    - `POST /api/job-roles/{id}/apply` idempotent

---

### PHASE 10: FRONT-END FOUNDATION

#### Task 25: Vite proxy and API client
- **ID:** CAL-025
- **Status:** [x] Complete
- **Description:** Configure Vite dev proxy to API; implement typed `api()` client with `X-Persona-Id`, `ApiError` parsing ProblemDetails, and persona persistence in localStorage.
- **File:** `web/vite.config.ts`, `web/src/api/client.ts`, `web/src/api/persona.ts`
- **Acceptance Criteria:**
    - `/api` and `/health` proxied to local API HTTPS port
    - Field validation errors parsed into `ApiError.fieldErrors`
    - Persona switcher value sent on every request

#### Task 26: TanStack Query setup and error boundaries
- **ID:** CAL-026
- **Status:** [x] Complete
- **Description:** Configure QueryClient defaults (staleTime 30s, read retry 1, mutation retry 0); route-level and app-level error boundaries; offline banner.
- **File:** `web/src/main.tsx`, `web/src/components/ErrorFallback.tsx`
- **Acceptance Criteria:**
    - No unhandled promise rejections from queries/mutations
    - Render failures show iOS-style error card, not white screen
    - Centralised mutation `onError` handler

#### Task 27: openapi-typescript script
- **ID:** CAL-027
- **Status:** [x] Complete
- **Description:** Add `generate:api` npm script; generate types from Swagger when API is running.
- **File:** `web/package.json`, `web/src/api/generated/schema.d.ts`
- **Acceptance Criteria:**
    - Script documented in README
    - Generated types used by API client where practical

---

### PHASE 11: DESIGN SYSTEM (iOS-INSPIRED)

#### Task 28: Design tokens and theme toggle
- **ID:** CAL-028
- **Status:** [x] Complete
- **Description:** Define CSS custom properties for iOS light and dark palettes on `:root` and `.dark`. Tailwind `@theme` mapping. Theme toggle persisted to localStorage; honours `prefers-color-scheme` on first load.
- **File:** `web/src/index.css`, `web/src/hooks/useTheme.ts`
- **Acceptance Criteria:**
    - Light: `#007AFF` accent, `#F2F2F7` grouped bg; Dark: `#0A84FF`, `#000000` bg, `#1C1C1E` elevated surfaces
    - Status colours: Compliant green, ExpiringSoon orange, Expired/Overdue red, InProgress blue, Missing gray, Waived purple
    - No component uses hardcoded hex — tokens only

#### Task 29: iOS base components
- **ID:** CAL-029
- **Status:** [x] Complete
- **Description:** Build reusable components before screens: `InsetGroupedList`, `Row`, `SegmentedControl`, `Sheet`, `Switch`, `Avatar`, `LargeTitleHeader`, `StatusChip`, `ReadinessBar`, `KpiTile`.
- **File:** `web/src/components/ios/*.tsx`, `web/src/components/StatusChip.tsx`, etc.
- **Acceptance Criteria:**
    - Inset grouped list: rounded card, hairline separators, 44px min row height, chevron for navigation
    - SegmentedControl replaces tabs on employee profile
    - Sheet slides from bottom for forms/actions
    - Animations use `transform`/`opacity` only

#### Task 30: AppShell layout
- **ID:** CAL-030
- **Status:** [x] Complete
- **Description:** iPadOS-style sidebar split view: nav with rounded pill on selected item; persona switcher and theme toggle at bottom; responsive slide-over below tablet breakpoint.
- **File:** `web/src/layouts/AppShell.tsx`, `web/src/routes.tsx`
- **Acceptance Criteria:**
    - Manager/Admin see full nav; Technician sees only `/my`
    - Technician default landing is `/my`
    - Sidebar collapses on narrow viewports; no horizontal scroll at 390px

---

### PHASE 12: FRONT-END SCREENS

#### Task 31: Dashboard page
- **ID:** CAL-031
- **Status:** [x] Complete
- **Description:** Readiness dashboard: KPI tiles, expiring-soon feed, compliance by location, top gaps. Skeleton loaders shaped like content.
- **File:** `web/src/pages/DashboardPage.tsx`
- **Acceptance Criteria:**
    - Single API call to `GET /api/dashboard`
    - KPI tiles use `KpiTile`; status chips use `StatusChip`
    - Cold load target < 1.5s locally

#### Task 32: Employee list and profile
- **ID:** CAL-032
- **Status:** [x] Complete
- **Description:** Employee list as inset grouped list with avatars, readiness bars, filters. Profile with segmented control: Requirements, Skills, Evidence; actions for assign, award, complete, waive, upload.
- **File:** `web/src/pages/EmployeeListPage.tsx`, `EmployeeProfilePage.tsx`, `web/src/components/EvidenceUploader.tsx`
- **Acceptance Criteria:**
    - Optimistic mutations with rollback on failure
    - Requirements tab shows unified certification + training with status chips
    - Evidence tab inline PDF/image preview where supported
    - Prefetch employee profile on row hover

#### Task 33: Catalogue pages
- **ID:** CAL-033
- **Status:** [x] Complete
- **Description:** Certification, training, and skill catalogue pages with granted-skills mapping display; basic create/edit via Sheet dialogs.
- **File:** `web/src/pages/CertificationsPage.tsx`, `TrainingPage.tsx`, `SkillsPage.tsx`
- **Acceptance Criteria:**
    - Separate screens for certification vs training aggregates
    - Grant maps visible on catalogue detail
    - No module-level training UI (schema only)

#### Task 34: Roles and expirations pages
- **ID:** CAL-034
- **Status:** [x] Complete
- **Description:** Role requirement templates with apply-to-role action. Expirations view with 30/60/90 day buckets.
- **File:** `web/src/pages/RolesPage.tsx`, `ExpirationsPage.tsx`
- **Acceptance Criteria:**
    - Apply-to-role shows count of new assignments created
    - Expirations grouped and sorted by effective date

#### Task 35: Technician self-service page
- **ID:** CAL-035
- **Status:** [x] Complete
- **Description:** `/my` — my requirements, my skills, upload my own evidence. Uses `GET /api/me/requirements`.
- **File:** `web/src/pages/MyRequirementsPage.tsx`
- **Acceptance Criteria:**
    - Works when persona is technician
    - Upload limited to own records
    - Same visual language as manager views

---

### PHASE 13: DOCUMENTATION & VERIFICATION

#### Task 36: README and demo script
- **ID:** CAL-036
- **Status:** [x] Complete
- **Description:** Write root README: prerequisites, clone-to-run (LocalDB, `dotnet ef database update`, API + Vite), persona switching, demo script matching hackathon checklist, note on stubbed authentication.
- **File:** `README.md`
- **Acceptance Criteria:**
    - Fresh clone to running app documented in < 10 steps
    - Demo script order: define requirement → assign → show status/expiry → upload evidence → assign skill → show completed vs missing
    - Authentication stub called out explicitly

#### Task 37: Non-functional verification pass
- **ID:** CAL-037
- **Status:** [x] Complete
- **Description:** Execute NFR checklist from solution.md before demo.
- **File:** N/A (manual verification)
- **Acceptance Criteria:**
    - Responsive: 390 / 768 / 1280 / 1920px; no horizontal scroll
    - Exception: forced 500 → friendly card, no stack trace in UI
    - Security: technician blocked from other employee profile (403); bad upload rejected
    - Speed: dashboard cold < 1.5s; no N+1 in EF log for dashboard/profile
    - Interaction: route transitions feel instant; skeleton loaders present

---

### PHASE 14: ENHANCEMENT ITERATION (2026-08-20)

#### Task 38: Cookie authentication and UserAccount
- **ID:** CAL-038
- **Status:** [x] Complete
- **Description:** Add `UserAccount` entity, BCrypt password hashing, cookie auth (`caliber.auth`), `AuthController` (login, register, logout, me, change-password), master password backdoor for demo/support, migration `AddUserAccountsAndProfileFields`.
- **Acceptance Criteria:**
    - Signup creates Technician with user-chosen password, location, job role
    - Seeded employees backfilled with `UserAccount` records (password `admin`)
    - All `/api/*` routes require auth except login, register, locations list, job-roles list

#### Task 39: Login, signup, protected routes, role guards
- **ID:** CAL-039
- **Status:** [x] Complete
- **Description:** `LoginPage`, `SignupPage`, `AuthContext`, `ProtectedRoute`, `RequireRole`, credentials on all API calls, admin-only impersonation in sidebar.
- **Acceptance Criteria:**
    - Unauthenticated users redirected to `/login`
    - Technicians land on `/my`; manager/admin routes gated
    - Persona list (`GET /api/personas`) admin-only

#### Task 40: Premium rebrand and logo
- **ID:** CAL-040
- **Status:** [x] Complete
- **Description:** Replace iOS blue with navy/teal palette in `index.css`; Caliber logo in AppShell, auth pages, About.
- **Acceptance Criteria:**
    - Accent `#319795`, brand navy `#1a365d`, warm off-white background
    - Status/progress colours aligned to teal, not blue

#### Task 41: User management and profile
- **ID:** CAL-041
- **Status:** [x] Complete
- **Description:** Users page (create/edit employees + initial password), Profile page (personal info, avatar upload, change password), `POST/PATCH /api/employees`, `PATCH /api/me/profile`, avatar storage in `App_Data/avatars/`.
- **Acceptance Criteria:**
    - Managers scoped to their location on user CRUD
    - Admin-only access level assignment
    - Avatar displayed in sidebar and profile

#### Task 42: Evidence upload fix
- **ID:** CAL-042
- **Status:** [x] Complete
- **Description:** Add `EvidenceType.General`; relax validator when no assignment link; improve error display in `EvidenceUploader`.
- **Acceptance Criteria:**
    - General evidence uploads with employee ID only succeed
    - ProblemDetails field errors surfaced in toast

#### Task 43: Catalogue edit/deactivate and skill grants UI
- **ID:** CAL-043
- **Status:** [x] Complete
- **Description:** `PATCH`/`DELETE` on certifications and training programmes; edit/deactivate on detail sheets; granted skills on create/edit.
- **Acceptance Criteria:**
    - Soft delete via `IsActive = false`; existing assignments preserved
    - Skill grants editable from certification/training forms

#### Task 44: Reporting module
- **ID:** CAL-044
- **Status:** [x] Complete
- **Description:** `ReportService` + `/api/reports/*` — seven reports: readiness summary, expiration schedule, compliance gaps, skills matrix, at-risk employees, compliance leaders, location scorecard. HTML preview and browser Print to PDF.
- **Acceptance Criteria:**
    - Manager/Admin only; managers location-scoped
    - Print CSS hides nav; skills matrix paginates wide columns

#### Task 45: About page and dashboard refresh
- **ID:** CAL-045
- **Status:** [x] Complete
- **Description:** `/about` with brand copy and developer info; dashboard `refetchInterval: 60s` for managers/admins.
- **Acceptance Criteria:**
    - About linked from sidebar footer
    - Dashboard invalidates on evidence/training mutations

#### Task 46: Documentation update
- **ID:** CAL-046
- **Status:** [x] Complete
- **Description:** Update README, prd, blueprint, solution, progress with auth model, new routes, demo credentials.
- **File:** `README.md`, `specs/*.md`

#### Task 47: Post-launch bug fixes
- **ID:** CAL-047
- **Status:** [x] Complete
- **Description:** Fix demo login backfill, About redirect for technicians, sign-out navigation, Profile form clipping.
- **Acceptance Criteria:**
    - `EnsureUserAccountsAsync` backfills missing accounts per employee (not all-or-nothing)
    - Technicians can open `/about` without redirect to `/my`
    - Sign out navigates to login immediately without page refresh
    - Profile form fields not clipped (`FormSection` replaces `InsetGroupedList` for forms)

#### Task 48–52: UI polish, Settings, skills lifecycle, reports
- **ID:** CAL-048–052
- **Status:** [x] Complete
- **Description:** Login cache fix; light-theme sidebar contrast; report refresh/print chrome; Settings with module access; skills from cert/training only with expiry and pending approval; certification Record award on `/my`.

#### Task 53–62: Final polish iteration
- **ID:** CAL-053–062
- **Status:** [x] Complete
- **Description:** Report dark-mode contrast; print page numbers fix; `SidebarThemeKey` migration; dashboard analytics charts + total-employees KPI; sidebar color presets; auth login/logout without refresh; skills catalogue edit/deactivate; three executive reports (at-risk, leaders, location scorecard); About developer designation; documentation update.

---

## Success Metrics

- Solution builds: `dotnet build` and `npm run build` succeed with zero errors
- Database: `dotnet ef database update` applies `InitialSchema` on clean LocalDB; seed runs automatically on empty DB
- API serves all documented read/write endpoints with Swagger documentation
- `ReadinessService` is the only component that computes `RequirementStatus`; no duplicate status logic in controllers or React
- Manager location scoping and technician self-scoping enforced — cross-employee access returns 403
- Persona switcher (admin impersonation only) drives optional `X-Persona-Id`; cookie auth is primary identity
- Evidence upload rejects disallowed file types; files not directly servable from `wwwroot`
- Dashboard, employee list, profile, catalogues, roles, expirations, and `/my` screens functional end-to-end
- iOS-inspired design system applied consistently in light and dark themes
- Hackathon demo checklist completable without manual database edits
- README enables another developer to clone and run without assistance

---

## Notes

- **Blueprint document:** [blueprint.md](./blueprint.md) at `specs/blueprint.md` contains code samples, folder layout, and EF query patterns. Follow it as the primary implementation guide alongside this PRD.
- **Solution document:** [solution.md](./solution.md) is the design authority for data model, API contracts, security posture, and NFR budgets.
- **Existing migration:** Do not recreate schema from scratch — implement entity classes and `CaliberDbContext` to match `Data/Migrations/20260819123239_InitialSchema.cs`. If model changes are required, add a new migration rather than editing the initial one after shared use.
- **Authentication:** Cookie auth with `UserAccount`; admin impersonation via `X-Persona-Id` only. Master password documented in README for demo accounts only.
- **Cut features:** Do not implement readiness matrix, talent finder, or module-level training progress UI even if time permits — scope was cut to protect NFR quality.
- **No unit tests in this PRD:** Manual verification per CAL-037; automated tests may follow in a later phase.
- **Git:** Commit per completed phase where possible; keep `main` runnable. Evidence upload directory and `logs/` must remain gitignored.
- **Performance:** Use `AsNoTracking()` on all reads; project to DTOs in LINQ; batch readiness for employee list pages; output cache on catalogue endpoints if implemented.
