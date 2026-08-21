# Caliber Web Frontend

React 19 + Vite + Tailwind CSS 4 + TanStack Query front end for the Caliber workforce readiness app.

## Development

```powershell
cd web
npm install
npm run dev
```

Dev server: `http://localhost:5173` — proxies `/api` and `/health` to `https://localhost:7143`.

Start the API first:

```powershell
cd ../src/Caliber.Api
dotnet run --launch-profile https
```

## Scripts

| Command | Purpose |
|---------|---------|
| `npm run dev` | Vite dev server with HMR |
| `npm run build` | Typecheck + production build |
| `npm run generate:api` | Regenerate OpenAPI types (API must be running) |

## Architecture

- **`src/api/`** — TanStack Query hooks and typed API client (`credentials: 'include'`)
- **`src/api/notifications.ts`**, **`src/api/renewals.ts`** — Activity feed and renewal request hooks
- **`src/lib/authSession.ts`** — Synchronous auth session store (fixes post-login redirect)
- **`src/components/NotificationBell.tsx`** — Header bell, broadcast sheet, dashboard notify button
- **`src/components/catalogue/GrantedSkillsEditor.tsx`** — Skill grant picker on cert/training forms
- **`src/contexts/`** — `AuthContext`, `PersonaContext` (admin impersonation via `X-Persona-Id`)
- **`src/components/ios/`** — iOS-inspired UI primitives (InsetGroupedList, Sheet, KpiTile, etc.)
- **`src/components/dashboard/`** — Recharts analytics widgets
- **`src/components/reports/`** — Report viewer with print/PDF export
- **`src/styles/reports.css`** — Screen preview uses theme CSS variables; print block is always light paper
- **`src/lib/sidebarThemes.ts`** — Sidebar color preset definitions

## Theming

Light/dark mode toggled from sidebar footer. Tokens defined in `src/index.css` as `--color-*` variables. Report preview inherits these for dark-mode legibility.

## Reports

Seven manager/admin reports on `/reports`: readiness summary, at-risk employees, compliance leaders, location scorecard, expiration schedule, compliance gaps, skills matrix. Each shows a **company name + report title** header (org name from Settings) and supports search, location filter, and **Export PDF** via browser print.

## Auth flow

Login/signup set auth cache synchronously and navigate immediately — no refresh required. Logout clears the full query cache and persona state before redirecting to `/login`.

## Demo credentials

See root [`README.md`](../README.md).
