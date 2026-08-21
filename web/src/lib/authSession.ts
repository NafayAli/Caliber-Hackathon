import type { AuthUser } from '../api/auth'

type Listener = () => void

let sessionUser: AuthUser | null | undefined = undefined
const listeners = new Set<Listener>()

export function getAuthSessionUser(): AuthUser | null | undefined {
  return sessionUser
}

export function setAuthSessionUser(user: AuthUser | null): void {
  sessionUser = user
  listeners.forEach((listener) => listener())
}

export function subscribeAuthSession(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
