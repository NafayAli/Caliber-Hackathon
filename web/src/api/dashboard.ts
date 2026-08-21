import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import type { ReadinessStatus } from '../components/StatusChip'
import { isManagerOrAdmin, useAuth } from '../contexts/AuthContext'
import { usePersona } from '../contexts/PersonaContext'

export type RequirementKind = 'Certification' | 'Training' | 'Skill'

export interface ExpiringItemDto {
  employeeId: number
  employeeName: string
  locationName: string
  kind: RequirementKind
  requirementName: string
  effectiveDate: string
  status: ReadinessStatus
}

export interface LocationComplianceDto {
  locationId: number
  locationName: string
  compliancePercent: number
  employeeCount: number
}

export interface GapItemDto {
  employeeId: number
  employeeName: string
  locationName: string
  kind: RequirementKind
  requirementName: string
  status: ReadinessStatus
}

export interface StatusBreakdownDto {
  status: ReadinessStatus
  count: number
}

export interface KindBreakdownDto {
  kind: RequirementKind
  count: number
}

export interface RenewalHorizonDto {
  label: string
  count: number
}

export interface DashboardDto {
  totalEmployees: number
  overallCompliancePercent: number
  employeesFullyReady: number
  fullyReadyPercent: number
  expiringWithin60Days: number
  expiredOrOverdue: number
  expiringSoonFeed: ExpiringItemDto[]
  byLocation: LocationComplianceDto[]
  topGaps: GapItemDto[]
  statusBreakdown: StatusBreakdownDto[]
  openGapsByKind: KindBreakdownDto[]
  renewalHorizon: RenewalHorizonDto[]
}

export function fetchDashboard(): Promise<DashboardDto> {
  return api<DashboardDto>('/api/dashboard')
}

export function useDashboard() {
  const { personaId } = usePersona()
  const { accessLevel } = useAuth()

  return useQuery({
    queryKey: ['dashboard', personaId],
    queryFn: fetchDashboard,
    enabled: personaId != null,
    refetchInterval: isManagerOrAdmin(accessLevel) ? 60_000 : false,
  })
}
