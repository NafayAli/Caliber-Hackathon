import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import type { ReadinessStatus } from '../components/StatusChip'
import type { ExpiringItemDto, GapItemDto } from './dashboard'
import type { SkillCategory, ProficiencyLevel } from './catalogues'
import { usePersona } from '../contexts/PersonaContext'

export interface EmployeeReadinessSummary {
  employeeId: number
  employeeName: string
  locationName: string
  readinessPercent: number
  worstStatus: ReadinessStatus
  gapCount: number
}

export interface ReadinessSummaryReport {
  overallCompliancePercent: number
  totalEmployees: number
  employeesFullyReady: number
  employees: EmployeeReadinessSummary[]
}

export interface ExpirationBucket {
  days: number
  label: string
  items: ExpiringItemDto[]
}

export interface ExpirationScheduleReport {
  totalExpiring: number
  buckets: ExpirationBucket[]
}

export interface ComplianceGapsReport {
  totalGaps: number
  gaps: GapItemDto[]
}

export interface SkillColumn {
  skillId: number
  skillName: string
  category: SkillCategory
}

export interface SkillsMatrixRow {
  employeeId: number
  employeeName: string
  locationName: string
  cells: SkillsMatrixCell[]
}

export interface SkillsMatrixCell {
  skillId: number
  proficiencyLevel: ProficiencyLevel | null
}

export interface SkillsMatrixReport {
  skills: SkillColumn[]
  rows: SkillsMatrixRow[]
}

export interface AtRiskEmployeeRow {
  employeeId: number
  employeeName: string
  locationName: string
  readinessPercent: number
  expiredCount: number
  overdueCount: number
  missingCount: number
  worstStatus: ReadinessStatus
  topGapName: string
  riskScore: number
}

export interface AtRiskEmployeesReport {
  totalAtRisk: number
  criticalCount: number
  avgReadinessPercent: number
  employees: AtRiskEmployeeRow[]
}

export interface ComplianceLeaderRow {
  employeeId: number
  employeeName: string
  locationName: string
  tier: string
  readinessPercent: number
  worstStatus: ReadinessStatus
}

export interface ComplianceLeadersReport {
  fullyReadyCount: number
  goldCount: number
  silverCount: number
  readyCount: number
  workforceReadyPercent: number
  leaders: ComplianceLeaderRow[]
}

export interface LocationScorecardRow {
  locationId: number
  locationName: string
  rank: number
  employeeCount: number
  fullyReadyCount: number
  atRiskCount: number
  expiringSoonCount: number
  compliancePercent: number
  fullyReadyPercent: number
}

export interface LocationScorecardReport {
  orgCompliancePercent: number
  topLocationName: string | null
  bottomLocationName: string | null
  locations: LocationScorecardRow[]
}

export type ReportKind =
  | 'readiness-summary'
  | 'expiration-schedule'
  | 'compliance-gaps'
  | 'skills-matrix'
  | 'at-risk-employees'
  | 'compliance-leaders'
  | 'location-scorecard'

export const reportKeys = {
  all: ['reports'] as const,
  kind: (personaId: number | null, kind: ReportKind) =>
    [...reportKeys.all, kind, personaId] as const,
}

export function fetchReadinessSummary(): Promise<ReadinessSummaryReport> {
  return api<ReadinessSummaryReport>('/api/reports/readiness-summary')
}

export function fetchExpirationSchedule(): Promise<ExpirationScheduleReport> {
  return api<ExpirationScheduleReport>('/api/reports/expiration-schedule')
}

export function fetchComplianceGaps(): Promise<ComplianceGapsReport> {
  return api<ComplianceGapsReport>('/api/reports/compliance-gaps')
}

export function fetchSkillsMatrix(): Promise<SkillsMatrixReport> {
  return api<SkillsMatrixReport>('/api/reports/skills-matrix')
}

export function fetchAtRiskEmployees(): Promise<AtRiskEmployeesReport> {
  return api<AtRiskEmployeesReport>('/api/reports/at-risk-employees')
}

export function fetchComplianceLeaders(): Promise<ComplianceLeadersReport> {
  return api<ComplianceLeadersReport>('/api/reports/compliance-leaders')
}

export function fetchLocationScorecard(): Promise<LocationScorecardReport> {
  return api<LocationScorecardReport>('/api/reports/location-scorecard')
}

function useReportQuery<T>(kind: ReportKind, queryFn: () => Promise<T>) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: reportKeys.kind(personaId, kind),
    queryFn,
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

export function useReadinessSummaryReport() {
  return useReportQuery('readiness-summary', fetchReadinessSummary)
}

export function useExpirationScheduleReport() {
  return useReportQuery('expiration-schedule', fetchExpirationSchedule)
}

export function useComplianceGapsReport() {
  return useReportQuery('compliance-gaps', fetchComplianceGaps)
}

export function useSkillsMatrixReport() {
  return useReportQuery('skills-matrix', fetchSkillsMatrix)
}

export function useAtRiskEmployeesReport() {
  return useReportQuery('at-risk-employees', fetchAtRiskEmployees)
}

export function useComplianceLeadersReport() {
  return useReportQuery('compliance-leaders', fetchComplianceLeaders)
}

export function useLocationScorecardReport() {
  return useReportQuery('location-scorecard', fetchLocationScorecard)
}

export const REPORT_META: Record<ReportKind, { title: string; description: string }> = {
  'readiness-summary': {
    title: 'Readiness summary',
    description: 'Compliance and readiness by employee',
  },
  'expiration-schedule': {
    title: 'Expiration schedule',
    description: 'Upcoming renewals in 30, 60, and 90 days',
  },
  'compliance-gaps': {
    title: 'Compliance gaps',
    description: 'Missing, overdue, and in-progress requirements',
  },
  'skills-matrix': {
    title: 'Skills matrix',
    description: 'Proficiency grid across active skills',
  },
  'at-risk-employees': {
    title: 'At-risk employees',
    description: 'Prioritized watchlist by compliance risk score',
  },
  'compliance-leaders': {
    title: 'Compliance leaders',
    description: 'Top performers fully up to date on requirements',
  },
  'location-scorecard': {
    title: 'Location scorecard',
    description: 'Site-by-site compliance ranking and KPIs',
  },
}
