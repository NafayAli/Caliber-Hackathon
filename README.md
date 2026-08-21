# Caliber — Workforce Readiness

Caliber is a hackathon demo web app for equipment dealerships. It unifies **certifications**, **training**, **skills**, and **evidence** into a single **readiness** view for managers and a self-service `/my` experience for technicians.

**Stack:** ASP.NET Core 8 Web API · EF Core · SQL Server LocalDB · React 19 · Vite · Tailwind CSS 4 · TanStack Query

> **Authentication:** Cookie-based login with email/password. Sign up creates a Technician account. Seeded demo accounts use password `admin`. Admin-only persona switcher supports impersonation for demos.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm
- **SQL Server LocalDB** (included with Visual Studio / SQL Server Express)
- Trust the ASP.NET dev HTTPS certificate (first run may prompt):
  ```powershell
  dotnet dev-certs https --trust
  ```

---

## Clone to run (< 10 steps)

1. **Clone** the repository and open a terminal at the repo root (`Caliber/`).

2. **Restore & build the API:**
   ```powershell
   dotnet build Caliber.sln
   ```

3. **Apply the database schema** (optional — the API also migrates on startup):
   ```powershell
   dotnet ef database update --project src/Caliber.Api
   ```

4. **Start the API** (HTTPS profile — Vite proxies to this port):
   ```powershell
   cd src/Caliber.Api
   dotnet run --launch-profile https
   ```
   Swagger: `https://localhost:7143/swagger` · Health: `https://localhost:7143/health`

5. **Install front-end dependencies** (new terminal):
   ```powershell
   cd web
   npm install
   ```

6. **Start the web app:**
   ```powershell
   npm run dev
   ```

7. **Open** `http://localhost:5173` in your browser.

8. **Sign in** or **create an account** at `/login` or `/signup`. Demo accounts:

   | Role | Email | Password |
   |------|-------|----------|
   | **Admin** | `marcus.chen@caliber-demo.com` | `admin` |
   | **Manager** | `sarah.mitchell@caliber-demo.com` | `admin` |
   | **Technician** | `jake.morrison@caliber-demo.com` | `admin` |

   New signups choose their own password and select location + job role.

   **Troubleshooting demo logins:** Restart the API after pulling latest code — in Development, startup detects a stale demo database (old `@caliberdealer.com` emails) and recreates it with the README accounts. Startup also backfills missing `UserAccount` rows for seeded employees. To reset manually: `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE Caliber"` then restart the API.

   **After sign-in:** You land in the app immediately — no manual refresh required (session store + explicit navigation). Sign out returns to login instantly.

   **Notifications:** Bell icon in the header shows unread activity (expiry alerts, reminders, acknowledgements, renewal decisions). Managers/admins can **broadcast announcements** from the panel, **Accept/Reject** renewal requests inline, and **Notify** individual employees from dashboard gap/expiring lists.

   **Renewals:** Expiring or expired certifications/training can be **renewed** by managers/admins from the employee profile. Technicians can **request renewal** from `/my`; managers approve or decline from pending renewal API / notifications.

   **Granted skills:** Edit skills granted by a certification or training program on the catalogue detail sheet and when creating a new item.

   **Settings** (Admin/Manager): org branding, optional contact fields, **11 sidebar color presets**, and per-role module access. If save fails with `Invalid column name 'SidebarThemeKey'`, restart the API or run `dotnet ef database update --project src/Caliber.Api`.

   **Skills catalogue:** Create, **edit**, and **deactivate** skills from the Skills page detail sheet.

   **Roles:** Create, **edit**, and **delete** job roles; manage requirement templates and apply to employees.

   **Evidence:** Upload certificate evidence when recording an award; preview from the certification row. PDFs preview inline (no auto-download). Managers verify evidence from the employee profile preview.

9. **Admin impersonation:** Admins see an **Impersonate** dropdown in the sidebar to view the app as another employee.

10. **Toggle theme** (light/dark) from the sidebar footer — preference is stored in `localStorage`.

11. **Dashboard:** Six KPI tiles (total employees, **average readiness**, fully ready count, **fully ready rate**, expiring, overdue). The **Fully ready** KPI deep-links to the compliance leaders report, which lists the same employees (Gold/Silver/Ready tiers). KPI tiles and chart cards are clickable and deep-link to employees, expirations, or reports. Four analytics charts plus expiring-soon and top-gaps feeds.

12. **Reports:** Every report shows a header with **organization name** (from Settings) and **report title** below it, on screen and in print/PDF export.

12. **Reports** (Manager/Admin): Seven HTML reports with search, location filter, and **Export PDF** (browser print). Dashboard links open reports via `?report=` query param.

13. **Regenerate API types** (optional, API must be running):
    ```powershell
    npm run generate:api
    ```

---

## Project layout

```
Caliber/
├── src/Caliber.Api/     # ASP.NET Core API, EF Core, seed data
├── web/                 # React + Vite front end
├── specs/               # PRD, blueprint, progress
└── Caliber.sln
```

Key docs: [`specs/prd.md`](specs/prd.md) · [`specs/blueprint.md`](specs/blueprint.md) · [`specs/solution.md`](specs/solution.md)

---

## Demo script (hackathon checklist)

Use a **Manager** persona unless noted. Total time ~8 minutes.

### 1. Define a requirement (catalogue)

1. Go to **Certifications** → **Add**.
2. Create a credential (e.g. code `DEMO-001`, issuing body `Internal`, 12-month validity).
3. Open the new item’s detail sheet — note **granted skills** if configured in seed data.

*Alternative (Admin):* **Roles** → open a job role → review template requirements.

### 2. Assign to an employee

1. **Employees** → open a technician at your location.
2. **Assign certification** or **Assign training** → pick from catalogue → confirm.
3. Requirements tab shows the new row with **Missing** or **Not started** status chip.

### 3. Show status & expiry

1. **Dashboard** — KPI tiles (total employees first), **Expiring soon** feed, **Top gaps**, and analytics charts.
2. **Expirations** — items grouped in **30 / 60 / 90 day** buckets, sorted by date.
3. Return to the employee profile — note due dates and computed readiness bar.

### 4. Upload evidence

1. On the employee profile → **Evidence** tab → **Upload evidence** (PDF/PNG/JPG/WebP).
2. Preview appears inline for PDF/images.
3. As manager: **Verify** the upload. As technician (`/my`): upload only — verification is manager-only.

### 5. Complete training / record award (status → compliant)

1. On the profile → **Requirements** → **Start** / **Mark complete** on a training row, or **Record award** on a certification.
2. Status chip updates to **Compliant** / **Expiring soon**; readiness bar increases.

### 6. Skills & completed vs missing

1. Profile **Skills** tab — skills granted automatically when linked certs/training complete.
2. **Dashboard → Top gaps** and employee list **worst status** chips show what is still **Missing**, **Overdue**, or **Expired**.

### 7. Technician self-service (switch to a Technician persona)

1. Persona switcher → pick any **Technician** — lands on **`/my`**.
2. View requirements, upload own evidence, start/complete training, **request renewal** on expiring items.
3. **Acknowledge** training when required — button disappears after acknowledgement; manager receives a notification.
4. Confirm manager nav items are hidden.

### 8. Notifications & renewals (Manager/Admin)

1. Open the **bell** in the header — review unread items; use **Broadcast** for team announcements.
2. **Dashboard** → **Top gaps** or **Expiring soon** → **Notify** on a row to ping that employee.
3. Employee profile → **Renew** on expiring/expired requirements, or review pending renewal requests via notifications.

---

## Authentication stub (important)

| What | Demo behaviour | Production replacement |
|------|------------------|------------------------|
| Sign-in | Persona switcher sets `X-Persona-Id` | Cookie / OIDC session |
| Header trust | Anyone who can reach the API can pick any persona id | Remove header; bind user from token |
| Enforcement | `PersonaMiddleware` → `ICurrentUser` → query scoping | Same seam, real auth populates `ICurrentUser` |

The header is read **only** in `PersonaMiddleware.cs`. The React client stores the selected id in `localStorage` and attaches it via `api/client.ts`.

---

## Configuration

Default connection string (`src/Caliber.Api/appsettings.json`):

```
Server=(localdb)\MSSQLLocalDB;Database=Caliber;Trusted_Connection=True;TrustServerCertificate=True
```

Evidence uploads: `App_Data/evidence/` (gitignored). Max 10 MB; allowed types validated by extension and magic bytes.

CORS allows `http://localhost:5173` (Vite dev server).

---

## Verification commands

```powershell
# Build
dotnet build Caliber.sln
cd web && npm run build

# Health
curl.exe -k https://localhost:7143/health

# Persona list (no header required)
curl.exe -k https://localhost:7143/api/personas

# Technician blocked from another employee (expect 403) — replace ids from /api/personas
curl.exe -k -H "X-Persona-Id: 3" https://localhost:7143/api/employees/4
```

---

## Non-functional checklist (CAL-037)

| Area | Status | Notes |
|------|--------|-------|
| **Build** | ✅ | `dotnet build` and `npm run build` — zero errors |
| **Responsive** | ✅ | AppShell slide-over at narrow widths; `max-w-*` content columns; no fixed horizontal overflow in layout CSS |
| **Exceptions** | ✅ | `GlobalExceptionHandler` → RFC 9457 ProblemDetails; React `ErrorBoundary` shows friendly card (no stack trace) |
| **Security** | ✅ | Technician cross-employee → 403 (`curl` verified); evidence magic-byte validation; security headers on pipeline |
| **Speed** | ✅ | Dashboard API ~220ms local; batched EF queries; skeleton loaders on data screens |
| **Interaction** | ✅ | Client-side routing; TanStack Query caching (30s stale); transform/opacity animations only |

See [`specs/solution.md`](specs/solution.md) for full NFR design intent.

---

## License

Hackathon / demo project — not production software.
