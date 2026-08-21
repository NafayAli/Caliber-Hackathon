import {
  Award,
  BarChart3,
  BookOpen,
  CalendarClock,
  GraduationCap,
  Info,
  LayoutDashboard,
  LogOut,
  Menu,
  Moon,
  Settings,
  Sun,
  UserCircle,
  UserCog,
  Users,
  Wrench,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useLogout } from '../api/auth'
import { MODULE_KEYS, useAppSettings, useMyModules } from '../api/settings'
import { isAdmin, isManagerOrAdmin, isTechnician, useAuth } from '../contexts/AuthContext'
import { usePersona } from '../contexts/PersonaContext'
import { useTheme } from '../hooks/useThemePreference'
import { cn } from '../lib/cn'
import { sidebarThemeStyle } from '../lib/sidebarThemes'
import { Avatar } from '../components/ios/Avatar'
import { NotificationBell } from '../components/NotificationBell'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  moduleKey: string
  adminOnly?: boolean
  managerOrAdmin?: boolean
  technicianVisible?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, moduleKey: MODULE_KEYS.Dashboard, technicianVisible: false },
  { to: '/employees', label: 'Employees', icon: Users, moduleKey: MODULE_KEYS.Employees, technicianVisible: false },
  { to: '/users', label: 'Users', icon: UserCog, moduleKey: MODULE_KEYS.Users, managerOrAdmin: true, technicianVisible: false },
  { to: '/certifications', label: 'Certifications', icon: Award, moduleKey: MODULE_KEYS.Certifications, technicianVisible: false },
  { to: '/training', label: 'Training', icon: GraduationCap, moduleKey: MODULE_KEYS.Training, technicianVisible: false },
  { to: '/skills', label: 'Skills', icon: Wrench, moduleKey: MODULE_KEYS.Skills, technicianVisible: false },
  { to: '/roles', label: 'Roles', icon: BookOpen, moduleKey: MODULE_KEYS.Roles, adminOnly: true, technicianVisible: false },
  { to: '/expirations', label: 'Expirations', icon: CalendarClock, moduleKey: MODULE_KEYS.Expirations, technicianVisible: false },
  { to: '/reports', label: 'Reports', icon: BarChart3, moduleKey: MODULE_KEYS.Reports, managerOrAdmin: true, technicianVisible: false },
  { to: '/settings', label: 'Settings', icon: Settings, moduleKey: MODULE_KEYS.Settings, managerOrAdmin: true, technicianVisible: false },
  { to: '/my', label: 'My requirements', icon: UserCircle, moduleKey: MODULE_KEYS.MyRequirements, technicianVisible: true },
]

function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const { user } = useAuth()
  const accessLevel = user?.accessLevel ?? null
  const { data: moduleData } = useMyModules()

  const enabledModules = useMemo(
    () => new Set(moduleData?.modules.filter((m) => m.isEnabled).map((m) => m.moduleKey) ?? []),
    [moduleData],
  )

  const items = useMemo(() => {
    const base = NAV_ITEMS.filter((item) => {
      if (enabledModules.size > 0 && !enabledModules.has(item.moduleKey)) return false
      if (isTechnician(accessLevel)) return item.technicianVisible
      if (item.adminOnly && !isAdmin(accessLevel)) return false
      if (item.managerOrAdmin && !isManagerOrAdmin(accessLevel)) return false
      return true
    })
    return base
  }, [accessLevel, enabledModules])

  return (
    <nav className="flex flex-1 flex-col gap-1 px-3">
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/'}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex min-h-11 items-center gap-3 rounded-xl px-3 text-[15px] font-medium transition-[background-color,opacity,transform]',
              isActive
                ? 'bg-accent text-white shadow-sm'
                : 'text-sidebar-text hover:bg-white/10',
            )
          }
        >
          <item.icon className="h-5 w-5 shrink-0" aria-hidden />
          {item.label}
        </NavLink>
      ))}
    </nav>
  )
}

function PersonaSwitcher() {
  const { user } = useAuth()
  const { personas, personaId, setPersonaId, activePersona } = usePersona()

  if (!isAdmin(user?.accessLevel ?? null)) {
    return null
  }

  return (
    <div className="space-y-2 border-t border-white/10 px-3 py-4">
      <label className="px-1 text-xs font-semibold uppercase tracking-wide text-sidebar-muted">
        Impersonate
      </label>
      <select
        value={personaId ?? ''}
        onChange={(event) => setPersonaId(Number(event.target.value))}
        className="w-full rounded-xl border border-white/15 bg-white/10 px-3 py-2 text-sm text-sidebar-text outline-none focus:ring-2 focus:ring-accent/40 [color-scheme:dark]"
      >
        {personas.map((persona) => (
          <option key={persona.id} value={persona.id}>
            {persona.displayName} ({persona.accessLevel})
          </option>
        ))}
      </select>
      {activePersona ? (
        <p className="px-1 text-xs text-sidebar-muted">
          {activePersona.jobRole} · {activePersona.location}
        </p>
      ) : null}
    </div>
  )
}

function ThemeToggle() {
  const { isDark, setPreference } = useTheme()

  return (
    <div className="flex items-center justify-between gap-3 px-4 py-3">
      <span className="text-sm text-sidebar-text">Appearance</span>
      <button
        type="button"
        onClick={() => setPreference(isDark ? 'light' : 'dark')}
        className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-white/10 px-3 text-sm text-sidebar-text ring-1 ring-white/15 transition-opacity hover:opacity-80"
      >
        {isDark ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
        {isDark ? 'Dark' : 'Light'}
      </button>
    </div>
  )
}

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { user } = useAuth()
  const { data: settings } = useAppSettings()
  const logout = useLogout()
  const navigate = useNavigate()

  async function handleLogout() {
    try {
      await logout.mutateAsync()
    } catch {
      // Still sign out locally if the API call fails (e.g. expired session).
    }
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center gap-3 px-4 py-5">
        <img src="/caliber-logo.svg" alt="" className="h-10 w-10 object-contain" />
        <div className="min-w-0 flex-1">
          <div className="text-lg font-semibold text-sidebar-text">
            {settings?.applicationName ?? 'Caliber'}
          </div>
          <div className="truncate text-xs text-sidebar-muted">
            {settings?.tagline ?? 'Calibration & Performance'}
          </div>
        </div>
      </div>

      {user ? (
        <Link
          to="/profile"
          onClick={onNavigate}
          className="mx-3 mb-2 flex items-center gap-3 rounded-xl px-3 py-2 hover:bg-white/10"
        >
          <Avatar name={user.displayName} src={user.avatarUrl} />
          <div className="min-w-0">
            <div className="truncate text-sm font-medium text-sidebar-text">{user.displayName}</div>
            <div className="truncate text-xs text-sidebar-muted">{user.jobRoleName}</div>
          </div>
        </Link>
      ) : null}

      <SidebarNav onNavigate={onNavigate} />

      <div className="mt-auto">
        <Link
          to="/about"
          onClick={onNavigate}
          className="mx-3 mb-2 flex min-h-10 items-center gap-3 rounded-xl px-3 text-sm text-sidebar-muted hover:bg-white/10 hover:text-sidebar-text"
        >
          <Info className="h-4 w-4" />
          About
        </Link>
        <ThemeToggle />
        <PersonaSwitcher />
        <button
          type="button"
          onClick={() => void handleLogout()}
          className="mx-3 mb-4 flex w-[calc(100%-1.5rem)] min-h-10 items-center gap-3 rounded-xl px-3 text-sm text-sidebar-muted hover:bg-white/10 hover:text-sidebar-text"
        >
          <LogOut className="h-4 w-4" />
          Sign out
        </button>
      </div>
    </div>
  )
}

export function AppShell({ children }: { children?: ReactNode }) {
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useAuth()
  const { data: settings } = useAppSettings()
  const { isDark } = useTheme()

  const sidebarStyle = useMemo(
    () => sidebarThemeStyle(settings?.sidebarThemeKey, isDark) as CSSProperties,
    [settings?.sidebarThemeKey, isDark],
  )

  const sidebarClassName =
    'sidebar-scroll shrink-0 overflow-y-auto border-r border-white/10 lg:sticky lg:top-0 lg:h-screen'

  useEffect(() => {
    if (!user) return

    const allowedPrefixes = ['/my', '/profile', '/about']
    const isAllowed = allowedPrefixes.some((prefix) => location.pathname.startsWith(prefix))

    if (isTechnician(user.accessLevel) && !isAllowed) {
      navigate('/my', { replace: true })
    }
  }, [user, location.pathname, navigate])

  return (
    <div className="min-h-screen bg-bg text-label lg:flex">
      <aside
        style={sidebarStyle}
        className={cn('hidden w-72 lg:block', sidebarClassName)}
        data-print-hide
      >
        <SidebarContent />
      </aside>

      {mobileOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button
            type="button"
            aria-label="Close navigation"
            className="absolute inset-0 bg-black/40 animate-fade-in"
            onClick={() => setMobileOpen(false)}
          />
          <aside
            style={sidebarStyle}
            className={cn('absolute inset-y-0 left-0 w-[min(100%,20rem)] shadow-xl animate-slide-up', sidebarClassName)}
          >
            <div className="flex justify-end p-3">
              <button
                type="button"
                aria-label="Close menu"
                className="rounded-full p-2 text-sidebar-text hover:bg-white/10"
                onClick={() => setMobileOpen(false)}
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            <SidebarContent onNavigate={() => setMobileOpen(false)} />
          </aside>
        </div>
      ) : null}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex items-center gap-3 border-b border-separator/50 bg-bg/90 px-4 py-3 backdrop-blur lg:hidden" data-print-hide>
          <button
            type="button"
            aria-label="Open navigation"
            className="rounded-xl p-2 hover:bg-elevated"
            onClick={() => setMobileOpen(true)}
          >
            <Menu className="h-5 w-5" />
          </button>
          <img src="/caliber-logo.svg" alt="Caliber" className="h-8 object-contain" />
          <div className="ml-auto">
            <NotificationBell />
          </div>
        </header>

        <div className="hidden lg:flex lg:justify-end lg:px-6 lg:pt-4" data-print-hide>
          <NotificationBell />
        </div>

        <main className="mx-auto w-full max-w-[90rem] flex-1 overflow-x-hidden px-4 py-6 sm:px-6">
          {children ?? <Outlet />}
        </main>
      </div>
    </div>
  )
}
