import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import type { RequirementStatus } from './employees'
import { usePersona } from '../contexts/PersonaContext'

export function useMyRequirements() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: ['me', 'requirements', personaId],
    queryFn: () => api<RequirementStatus[]>('/api/me/requirements'),
    enabled: personaId != null,
    staleTime: 30_000,
  })
}

export function computeReadinessPercent(requirements: RequirementStatus[]): number {
  const mandatory = requirements.filter((req) => req.isMandatory)
  if (mandatory.length === 0) return 100

  const compliant = mandatory.filter(
    (req) => req.status === 'Compliant' || req.status === 'ExpiringSoon',
  ).length

  return Math.round((100 * compliant) / mandatory.length * 10) / 10
}
