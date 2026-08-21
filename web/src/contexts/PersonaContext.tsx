import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { getPersonaId, setPersonaId as persistPersonaId, clearPersonaId } from '../api/persona'
import type { PersonaDto } from '../api/types'
import { isAdmin, useAuth } from './AuthContext'

interface PersonaContextValue {
  personas: PersonaDto[]
  personaId: number | null
  activePersona: PersonaDto | null
  effectiveEmployeeId: number | null
  setPersonaId: (id: number) => void
  isLoading: boolean
  isImpersonating: boolean
}

const PersonaContext = createContext<PersonaContextValue | null>(null)

export function PersonaProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const { user, isAuthenticated } = useAuth()
  const admin = isAdmin(user?.accessLevel ?? null)

  const { data: personas = [], isLoading } = useQuery({
    queryKey: ['personas'],
    queryFn: () => api<PersonaDto[]>('/api/personas'),
    enabled: isAuthenticated && admin,
  })

  const [personaId, setPersonaIdState] = useState<number | null>(() => {
    const stored = getPersonaId()
    return stored ? Number(stored) : null
  })

  useEffect(() => {
    if (!user) {
      clearPersonaId()
      setPersonaIdState(null)
      return
    }

    if (!admin) {
      clearPersonaId()
      setPersonaIdState(user.employeeId)
      return
    }

    if (personaId && personas.some((p) => p.id === personaId)) {
      return
    }

    const nextId = user.employeeId
    persistPersonaId(nextId)
    setPersonaIdState(nextId)
  }, [user, admin, personas, personaId])

  const value = useMemo<PersonaContextValue>(() => {
    const effectiveEmployeeId = admin ? (personaId ?? user?.employeeId ?? null) : (user?.employeeId ?? null)
    const activePersona = personas.find((p) => p.id === personaId) ?? (user ? {
      id: user.employeeId,
      displayName: user.displayName,
      accessLevel: user.accessLevel,
      jobRole: user.jobRoleName,
      location: user.locationName,
    } : null)

    return {
      personas,
      personaId: effectiveEmployeeId,
      activePersona,
      effectiveEmployeeId,
      isLoading,
      isImpersonating: admin && personaId !== user?.employeeId,
      setPersonaId: (id: number) => {
        persistPersonaId(id)
        setPersonaIdState(id)
        void queryClient.invalidateQueries()
      },
    }
  }, [personas, personaId, user, admin, isLoading, queryClient])

  return <PersonaContext.Provider value={value}>{children}</PersonaContext.Provider>
}

export function usePersona(): PersonaContextValue {
  const context = useContext(PersonaContext)
  if (!context) {
    throw new Error('usePersona must be used within PersonaProvider')
  }
  return context
}

export function useAccessLevel() {
  const { user } = useAuth()
  return user?.accessLevel ?? null
}

export { isTechnician, isManagerOrAdmin, isAdmin } from './AuthContext'
