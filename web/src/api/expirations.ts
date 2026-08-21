import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import type { ExpiringItemDto } from './dashboard'
import { usePersona } from '../contexts/PersonaContext'

export interface ExpirationBucket {
  days: number
  label: string
  items: ExpiringItemDto[]
}

export interface ExpirationsDto {
  buckets: ExpirationBucket[]
}

export function useExpirations() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: ['expirations', personaId],
    queryFn: () => api<ExpirationsDto>('/api/expirations'),
    enabled: personaId != null,
    staleTime: 30_000,
  })
}
