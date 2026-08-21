# Progress: Caliber — Workforce Readiness Web Application

## Overview
- **PRD:** [prd.md](./prd.md)
- **Blueprint:** [blueprint.md](./blueprint.md)
- **Solution:** [solution.md](./solution.md)
- **Started:** 2026-08-19
- **Status:** ✅ **COMPLETE** (37/37 PRD tasks + Enhancement + Final polish + Round 3 — 2026-08-21)

---

## Notifications, renewals & UX round 3 (2026-08-21)

| Area | Status | Notes |
|------|--------|-------|
| Login redirect fix | ✅ | `authSession` store + `navigate()` after login/signup |
| Notifications API | ✅ | `Notification` entity; broadcast, notify-employees, unread summary |
| Notification bell UI | ✅ | Header bell, mark read, manager broadcast sheet |
| Renewal in notifications | ✅ | Accept/Reject on renewal-request notifications; employee notified on decision |
| Dashboard notify | ✅ | **Notify** on top gaps + expiring soon rows |
| Granted skills editor | ✅ | Create + detail sheets for certifications and training |
| Renewal workflow | ✅ | Employee request; manager approve/decline/direct renew |
| Acknowledge UX | ✅ | Hide after acknowledged; notify managers/admins |
| Waive help text | ✅ | One-line helper on waive actions |
| Report headers | ✅ | Company name (Settings) + report title on all reports |
| Fully ready KPI alignment | ✅ | `IsEmployeeFullyReady` shared by dashboard + compliance leaders report |
| Docs | ✅ | README + specs + web/README updated |

---

## UI overhaul + round 2 (2026-08-20)

| Area | Status | Notes |
|------|--------|-------|
| Centered modals | ✅ | `Sheet.tsx` — centered popup replaces bottom sheets |
| Profile layout | ✅ | Two-column personal info + password on large screens |
| Evidence on certifications | ✅ | Evidence tab removed; award + upload; preview on cert rows |
| Clickable dashboard | ✅ | KPI tiles + chart cards navigate to reports/employees/expirations |
| Dashboard KPI clarity | ✅ | Average readiness + fully ready rate (6 tiles); `FullyReadyPercent` on API |
| Auth bootstrap | ✅ | Cached session + stable auth query; login/signup rely on auth state |
| PDF inline preview | ✅ | `/content` inline; `/download` for attachment |
| Sidebar themes | ✅ | Mustard, chocolate, orange added (11 presets) |
| Roles CRUD | ✅ | POST/PATCH/DELETE job roles; GET departments |
| UX copy | ✅ | Acknowledge helper; pending verification text on cert evidence |

---

## Final polish iteration (2026-08-20)

| Area | Status | Notes |
|------|--------|-------|
| Report dark mode | ✅ | Theme-aware CSS variables in `reports.css` |
| Print page numbers | ✅ | `@page` margin boxes; hidden on screen preview |
| Settings migration fix | ✅ | `SidebarThemeKey` + optional `ContactEmail` migrations registered |
| Sidebar color presets | ✅ | 11 themes in Settings → General |
| Dashboard analytics | ✅ | Recharts widgets, 6 KPI tiles (avg readiness + fully ready rate) |
| Auth navigation | ✅ | Login/logout without refresh; full cache clear on sign out |
| Skills edit/deactivate | ✅ | PATCH/DELETE on skill catalogue |
| New reports (3) | ✅ | At-risk employees, compliance leaders, location scorecard |
| About page | ✅ | Developer designation: Database Developer |
| Employee add skill | ✅ | Add skill sheet on employee profile |
| Documentation | ✅ | README + specs + web/README updated |

---

## Enhancement iteration (2026-08-20)

| Area | Status | Notes |
|------|--------|-------|
| Cookie auth + login/signup | ✅ | UserAccount, BCrypt, master password backdoor |
| Admin impersonation | ✅ | X-Persona-Id header, admin-only |
| Navy/teal rebrand | ✅ | index.css tokens, SVG logo |
| User management | ✅ | Users page, employee CRUD, initial password |
| Profile + avatar | ✅ | /profile, App_Data/avatars |
| Evidence fix | ✅ | General evidence type, better errors |
| Catalogue edit/deactivate | ✅ | PATCH/DELETE soft delete |
| Skill grants UI | ✅ | Edit flows on cert/training sheets |
| Reporting module | ✅ | 7 reports, HTML + print PDF |
| About page | ✅ | Developer info |
| Bug fixes (CAL-047) | ✅ | Demo login backfill, About redirect, logout, FormSection |

---

## Task Progress

| Task ID | Task Name | Status | Started | Completed | Notes |
|---------|-----------|--------|---------|-----------|-------|
| CAL-001 | Wire Program.cs and extension methods | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Extensions, Serilog, health, migrate/seed hook |
| CAL-002 | Create appsettings configuration | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | ConnectionStrings, Cors, Evidence section (+ AllowedExtensions), Serilog |
| CAL-003 | Global exception handler and domain exceptions | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `GlobalExceptionHandler` + `AppExceptions` in `Common/` |
| CAL-004 | Security headers, CORS, and rate limiting | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Headers in pipeline; CORS locked to Vite origin; 429 + Retry-After |
| CAL-005 | Create domain enums | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `Domain/Enums.cs` — uses `AccessLevel` + `ReadinessStatus` |
| CAL-006 | Create organisation entities | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `Domain/Organisation.cs` |
| CAL-007 | Create certification aggregate entities | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `Domain/Certifications.cs` |
| CAL-008 | Create training aggregate entities | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `Domain/Training.cs` |
| CAL-009 | Create skills, requirements, and evidence entities | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `Domain/Skills.cs`, `RoleRequirement.cs`, `Evidence.cs` |
| CAL-010 | Create CaliberDbContext | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Full Fluent config + audit stamping |
| CAL-011 | Implement ICurrentUser and PersonaMiddleware | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Added `EnsureCanAccessEmployee` |
| CAL-012 | Implement SeedData | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | 3 locations, 4 depts, 5 roles, 12 employees, staged statuses |
| CAL-013 | Implement ReadinessService core | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `RequirementStatusDto`, `ComputeStatus`, batch queries |
| CAL-014 | Dashboard and expirations queries | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `GetDashboardAsync`, `GetExpirationsAsync`, dashboard DTOs, location scoping |
| CAL-015 | Create request/response DTOs | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Common, Employees, Catalogues, Evidence, Requests, Personas |
| CAL-016 | FluentValidation validators | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | 14 validators + shared extensions; IClock for future dates |
| CAL-017 | CertificationService | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Catalogue CRUD, assign, award, waive, skill granting |
| CAL-018 | TrainingService | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Catalogue CRUD, assign, progress, complete, acknowledge |
| CAL-019 | SkillService and RoleRequirementService | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Skill assign, role templates, idempotent apply-to-role |
| CAL-020 | EmployeeService | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Paged list, profile, requirements, personas |
| CAL-021 | LocalFileEvidenceStorage | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Magic bytes, MIME/extension allowlist, GUID paths |
| CAL-022 | EvidenceService and EvidenceController | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Upload, download, verify, delete |
| CAL-023 | Read controllers | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Dashboard, employees, catalogues, expirations, me, personas |
| CAL-024 | Write controllers | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Assign, award, complete, waive, apply-to-role |
| CAL-025 | Vite proxy and API client | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Proxy, ApiError, persona localStorage |
| CAL-026 | TanStack Query setup and error boundaries | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | QueryClient, ErrorBoundary, OfflineBanner, sonner |
| CAL-027 | openapi-typescript script | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `npm run generate:api` + placeholder schema |
| CAL-028 | Design tokens and theme toggle | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | CSS vars, `@theme`, `useTheme` + toggle in AppShell |
| CAL-029 | iOS base components | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | InsetGroupedList, Sheet, SegmentedControl, StatusChip, KpiTile, etc. |
| CAL-030 | AppShell layout | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Sidebar nav, persona switcher, mobile slide-over, technician redirect |
| CAL-031 | Dashboard page | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | KPI tiles, expiring feed, gaps, location compliance, skeletons |
| CAL-032 | Employee list and profile | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | List filters, profile tabs, actions, evidence upload/preview, prefetch |
| CAL-033 | Catalogue pages | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Cert/training/skills lists, detail sheets, granted skills, create forms |
| CAL-034 | Roles and expirations pages | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Role templates, apply-to-role counts, 30/60/90 expirations buckets |
| CAL-035 | Technician self-service page | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | `/my` requirements/skills/evidence, training actions, own uploads |
| CAL-036 | README and demo script | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Root README.md — clone-to-run, demo script, auth stub |
| CAL-037 | Non-functional verification pass | ✅ COMPLETED | 2026-08-19 | 2026-08-19 | Builds, 403, dashboard latency, NFR checklist in README |

---

## Summary
- **Total Tasks:** 37
- **Completed:** 37
- **Partial:** 0
- **Not Started:** 0

---

## Log

### 2026-08-19 — Iteration 17 (CAL-036–CAL-037) — **PROJECT COMPLETE**
- Wrote root `README.md` — prerequisites, 10-step clone-to-run, demo script, auth stub, NFR checklist
- Fixed `ListPersonasAsync` to return all employees when unauthenticated (persona picker on fresh load)
- NFR verification: `dotnet build` + `npm run build` pass; technician 403 confirmed; dashboard API ~220ms
- **All 37 PRD tasks complete**

### 2026-08-19 — Iteration 16 (CAL-035)
- Created `api/me.ts` — `useMyRequirements`, readiness percent helper
- Built `MyRequirementsPage` — segmented Requirements/Skills/Evidence tabs matching manager profile UX
- Requirements from `GET /api/me/requirements`; skills/evidence from own employee profile
- Technician actions: start/complete/acknowledge training, upload/delete own evidence (no verify)
- Added `useAcknowledgeTraining` mutation; restored `useCompleteTraining`
- Removed empty `Placeholders.tsx`
- **Verification:** `npm run build` succeeded

### 2026-08-19 — Iteration 15 (CAL-034)
- Created `api/roles.ts` — job role templates, add requirement, apply-to-role mutation
- Created `api/expirations.ts` — 30/60/90 day bucket query
- Built `RolesPage` — template list, requirement detail sheet, add requirement, apply with created counts toast
- Built `ExpirationsPage` — grouped buckets sorted by effective date, status chips, employee navigation
- **Verification:** `npm run build` succeeded

### 2026-08-19 — Iteration 14 (CAL-033)
- Expanded `api/catalogues.ts` with full DTO types, detail queries, and create mutations
- Built `CertificationsPage`, `TrainingPage`, `SkillsPage` — searchable inset lists, detail sheets, create sheets
- Added `GrantedSkillsList` showing proficiency grants on certification/training detail
- Shared catalogue form field components
- **Verification:** `npm run build` succeeded

### 2026-08-19 — Iteration 13 (CAL-032)
- Added `RowVersion` to `RequirementStatusDto` for optimistic mutation concurrency
- Created `api/employees.ts` — list/profile queries, prefetch, optimistic assign/award/waive/complete mutations
- Created `api/catalogues.ts` — certification/training/job-role pickers
- Built `EmployeeListPage` — search, status/role/location filters, avatars, readiness bars, hover prefetch
- Built `EmployeeProfilePage` — segmented Requirements/Skills/Evidence tabs, Sheet actions, status chips
- Built `EvidenceUploader` — multipart upload with inline PDF/image preview
- **Verification:** `npm run build` and `dotnet build` succeeded

### 2026-08-19 — Iteration 12 (CAL-028–CAL-031)
- Fixed AppShell build errors (`useMemo` import, `useTheme` hook)
- Completed design tokens (`index.css`, `useTheme.ts`, `useThemePreference.ts`) and theme toggle in AppShell
- Built iOS component library: InsetGroupedList, Row, SegmentedControl, Sheet, Switch, Avatar, LargeTitleHeader, StatusChip, ReadinessBar, KpiTile
- Implemented AppShell with sidebar nav, persona switcher, responsive slide-over, technician-only `/my` nav + redirect
- Created `DashboardPage.tsx` wired to `GET /api/dashboard` — KPI tiles, expiring-soon feed, top gaps, compliance by location, content-shaped skeletons
- **Verification:** `npm run build` succeeded

### 2026-08-19 — Iteration 11 (CAL-025–CAL-027)
- Configured Vite proxy for `/api` and `/health` → `https://localhost:7143`
- Created `api/client.ts` (ApiError, field errors, X-Persona-Id), `api/persona.ts`, `api/types.ts`
- Wired TanStack Query defaults, ErrorBoundary, OfflineBanner, sonner toasts in `main.tsx`
- Added `npm run generate:api` + placeholder `api/generated/schema.d.ts`
- Replaced stock App with persona picker smoke test; refreshed `index.css` with Tailwind + base tokens
- **Verification:** `npm run build` succeeded

### 2026-08-19 — Iteration 10 (CAL-021–CAL-024)
- Implemented `LocalFileEvidenceStorage` + `EvidenceFileValidator` (PDF/PNG/JPEG/WebP, magic bytes, size cap)
- Added `PayloadTooLargeException`, `UnsupportedMediaTypeException`
- Created `EvidenceService` + `EvidenceController` (multipart upload, streaming download, verify, delete)
- Created read controllers: dashboard, expirations, employees, catalogues, job-roles, me, personas
- Created write routes: employee assignments, awards/waive, training progress/complete/acknowledge, role apply
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 9 (CAL-019, CAL-020)
- Created `Services/SkillService.cs` — catalogue CRUD, manual assign/reassess (`ManagerAssessed` / `Experience`)
- Created `Services/RoleRequirementService.cs` — role templates, add requirement, idempotent apply-to-role
- Added `ApplyRoleResultDto` to catalogue DTOs
- Created `Services/EmployeeService.cs` — paged list with batch readiness, profile, `/api/personas` data
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 8 (CAL-017, CAL-018)
- Created `Services/SkillGrantingHelper.cs` — certification + training skill grants
- Created `Services/CertificationService.cs` — catalogue, assign, award, waive
- Created `Services/TrainingService.cs` — catalogue, assign, progress, complete, acknowledge
- Registered both services in DI
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 7 (CAL-016)
- Created `Validators/CaliberValidationExtensions.cs` — row version, future date, notes length
- Created `Validators/WriteRequestValidators.cs` — all write request validators
- Created `Validators/EvidenceValidators.cs` — upload + verify
- Validators auto-registered via existing `AddValidatorsFromAssemblyContaining<Program>`
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 6 (CAL-015)
- Created DTO layer under `Dtos/`:
  - `Common/PagedResult.cs`, existing `RequirementStatusDto`
  - `Employees/EmployeeDtos.cs` — list query, list item, profile, assignment DTOs
  - `Catalogues/CatalogueDtos.cs` — certifications, training, skills, job roles, locations
  - `Evidence/EvidenceDtos.cs` — metadata + upload/verify requests
  - `Requests/WriteRequests.cs` — all write-path request bodies
  - `Personas/PersonaDto.cs` — persona switcher + current user
- OpenAPI endpoint listing completes when controllers land in CAL-023
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 5 (CAL-014)
- Created `Dtos/Dashboard/DashboardDtos.cs` — `DashboardDto`, `ExpiringItemDto`, `LocationComplianceDto`, `GapItemDto`, `ExpirationsDto`, `ExpirationBucketDto`
- Extended `ReadinessService` with `GetDashboardAsync` and `GetExpirationsAsync`
- Scoped employees via `ICurrentUser` (Admin all, Manager location, Technician self)
- Two bounded queries: scoped employees + batch requirements (no per-employee loops)
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 4 (CAL-013)
- Created `Dtos/Common/RequirementStatusDto.cs`
- Created `Services/ReadinessService.cs`
- Registered `ReadinessService` in DI
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 3 (CAL-012)
- Implemented full `SeedData.EnsureSeededAsync`
- Demo personas: Sarah Mitchell (manager), Jake Morrison (technician)
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 2 (CAL-002–CAL-011)
- Verified restored code; added `EnsureCanAccessEmployee`, Evidence `AllowedExtensions`, rate limiter `Retry-After`
- **Verification:** `dotnet build` succeeded

### 2026-08-19 — Iteration 1 (CAL-001)
- Extension methods, pipeline, migrate/seed hook
- **Verification:** `dotnet build` succeeded

### 2026-08-20 — UI & platform iteration (CAL-048–052)
- **Login:** `setQueryData` on login/signup — no refresh needed after sign-in
- **Theme:** Dark navy sidebar in light mode; accent-tinted card surfaces
- **Reports:** Refresh button; print header (org + title); footer (print date, user, page X of Y); copyright; higher contrast tables/KPIs
- **Settings:** Admin/manager Settings page — org name, email, module access matrix
- **Skills:** Removed direct role skill requirements; skills grant from cert/training with expiry; manual skill requests pending manager approval
- **`/my`:** Record award + waive for certifications (fixes Missing with no actions)
- **Verification:** `dotnet build` and `npm run build` succeeded

### 2026-08-20 — Bugfix iteration (CAL-047)
- **Demo logins:** `EnsureUserAccountsAsync` now backfills per employee instead of skipping when any account exists
- **About page:** Technicians allowed on `/about` (AppShell redirect guard)
- **Sign out:** `useLogout` clears auth state synchronously; navigate to login without refresh
- **Profile UI:** New `FormSection` component; Profile page forms no longer clipped by `overflow-hidden`
- **Verification:** `dotnet build` and `npm run build` succeeded

### 2026-08-20 — Enhancement iteration
- Cookie auth, UserAccount, login/signup, admin impersonation
- Users, Profile, Reports, About pages; catalogue edit/deactivate
- Navy/teal rebrand; evidence General type; skill grants UI
- Updated prd, blueprint, solution, README with demo credentials
- **Verification:** `dotnet build` and `npm run build` succeeded

### 2026-08-20 — Final polish iteration (CAL-053–062)
- Report dark-mode CSS; print page numbers via `@page`; settings migration fix
- Dashboard: total-employees KPI, Recharts analytics, wider layout
- Sidebar color presets; auth login/logout without refresh
- Skills catalogue PATCH/DELETE; 3 new executive reports
- About page: Database Developer designation
- Updated README, prd, blueprint, solution, progress, web/README

---

Enhancement iteration complete. Final polish (CAL-053–062) adds dashboard charts, sidebar themes, 3 executive reports, skills catalogue edit/deactivate, auth UX hardening, and report/settings bug fixes.

Optional follow-ups: automated tests, Aspen employee sync, server-side PDF export.
