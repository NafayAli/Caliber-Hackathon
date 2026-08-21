import { createContext, useContext, useMemo, useSyncExternalStore, type ReactNode } from 'react'
import { useAuthMe, type AuthUser } from '../api/auth'
import type { AccessLevel } from '../api/types'
import { getAuthSessionUser, subscribeAuthSession } from '../lib/authSession'

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  isAuthenticated: boolean
  accessLevel: AccessLevel | null
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const sessionUser = useSyncExternalStore(subscribeAuthSession, getAuthSessionUser, () => undefined)
  const { data: queryUser, isLoading, isFetched } = useAuthMe()

  const user = (sessionUser !== undefined ? sessionUser : queryUser) ?? null

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isLoading: isLoading && !isFetched && sessionUser === undefined && queryUser === undefined,
    isAuthenticated: !!user,
    accessLevel: user?.accessLevel ?? null,
  }), [user, isLoading, isFetched, sessionUser, queryUser])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}

export function isTechnician(level: AccessLevel | null): boolean {
  return level === 'Technician'
}

export function isManagerOrAdmin(level: AccessLevel | null): boolean {
  return level === 'Manager' || level === 'Admin'
}

export function isAdmin(level: AccessLevel | null): boolean {
  return level === 'Admin'
}
