import { useSyncExternalStore } from 'react'
import type { RequirementKind } from '../api/dashboard'
import type { ReadinessStatus } from '../components/StatusChip'
import { getResolvedTheme, subscribeThemeChange } from '../hooks/useTheme'

function cssVar(name: string, fallback: string): string {
  if (typeof document === 'undefined') {
    return fallback
  }

  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

export function getChartAccentColor(): string {
  return cssVar('--color-accent', '#319795')
}

export function getChartLabelColor(): string {
  return cssVar('--color-secondary-label', 'rgba(26, 32, 44, 0.6)')
}

export function getChartGridColor(): string {
  return cssVar('--color-separator', 'rgba(26, 54, 93, 0.12)')
}

const STATUS_COLOR_VARS: Record<ReadinessStatus, string> = {
  Compliant: '--color-status-compliant',
  ExpiringSoon: '--color-status-expiring',
  Expired: '--color-status-danger',
  Overdue: '--color-status-danger',
  InProgress: '--color-status-progress',
  Missing: '--color-status-missing',
  Waived: '--color-status-waived',
}

const STATUS_FALLBACKS: Record<ReadinessStatus, string> = {
  Compliant: '#38a169',
  ExpiringSoon: '#d69e2e',
  Expired: '#e53e3e',
  Overdue: '#e53e3e',
  InProgress: '#319795',
  Missing: '#718096',
  Waived: '#805ad5',
}

export const STATUS_LABELS: Record<ReadinessStatus, string> = {
  Compliant: 'Compliant',
  ExpiringSoon: 'Expiring soon',
  Expired: 'Expired',
  Overdue: 'Overdue',
  InProgress: 'In progress',
  Missing: 'Missing',
  Waived: 'Waived',
}

export function getStatusColor(status: ReadinessStatus): string {
  return cssVar(STATUS_COLOR_VARS[status], STATUS_FALLBACKS[status])
}

export const KIND_LABELS: Record<RequirementKind, string> = {
  Certification: 'Certification',
  Training: 'Training',
  Skill: 'Skill',
}

const KIND_COLORS: Record<RequirementKind, string> = {
  Certification: '#319795',
  Training: '#3182ce',
  Skill: '#805ad5',
}

export function getKindColor(kind: RequirementKind): string {
  return KIND_COLORS[kind]
}

export function useChartThemeSnapshot() {
  const resolvedTheme = useSyncExternalStore(
    subscribeThemeChange,
    getResolvedTheme,
    () => 'light' as const,
  )

  return {
    resolvedTheme,
    accent: getChartAccentColor(),
    label: getChartLabelColor(),
    grid: getChartGridColor(),
    statusColor: (status: ReadinessStatus) => getStatusColor(status),
    kindColor: (kind: RequirementKind) => getKindColor(kind),
  }
}

export function useChartTheme() {
  return useChartThemeSnapshot()
}
