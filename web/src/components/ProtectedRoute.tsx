import { Navigate, Outlet } from 'react-router-dom'
import { useAuth, isAdmin, isManagerOrAdmin } from '../contexts/AuthContext'
import type { AccessLevel } from '../api/types'

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-bg">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-accent border-t-transparent" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

interface RequireRoleProps {
  adminOnly?: boolean
  managerOrAdmin?: boolean
  children: React.ReactNode
}

export function RequireRole({ adminOnly, managerOrAdmin, children }: RequireRoleProps) {
  const { accessLevel, isLoading } = useAuth()

  if (isLoading) {
    return null
  }

  if (adminOnly && !isAdmin(accessLevel)) {
    return <Navigate to="/" replace />
  }

  if (managerOrAdmin && !isManagerOrAdmin(accessLevel)) {
    return <Navigate to="/my" replace />
  }

  return <>{children}</>
}

export function getDefaultRoute(accessLevel: AccessLevel | null): string {
  return accessLevel === 'Technician' ? '/my' : '/'
}
