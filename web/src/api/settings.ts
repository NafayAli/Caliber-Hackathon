import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type { AccessLevel } from './types'

export interface AppSettings {
  applicationName: string
  organizationName: string
  contactEmail?: string
  supportPhone?: string
  tagline?: string
  sidebarThemeKey?: string
}

export interface ModuleAccess {
  accessLevel: AccessLevel
  moduleKey: string
  isEnabled: boolean
}

export const settingsKeys = {
  all: ['settings'] as const,
  app: ['settings', 'app'] as const,
  modules: ['settings', 'modules'] as const,
  myModules: ['settings', 'modules', 'me'] as const,
}

export const MODULE_KEYS = {
  Dashboard: 'Dashboard',
  Employees: 'Employees',
  Users: 'Users',
  Certifications: 'Certifications',
  Training: 'Training',
  Skills: 'Skills',
  Roles: 'Roles',
  Expirations: 'Expirations',
  Reports: 'Reports',
  Settings: 'Settings',
  MyRequirements: 'MyRequirements',
  Profile: 'Profile',
  About: 'About',
} as const

export type ModuleKey = (typeof MODULE_KEYS)[keyof typeof MODULE_KEYS]

const ROUTE_MODULE: Record<string, ModuleKey> = {
  '/': MODULE_KEYS.Dashboard,
  '/employees': MODULE_KEYS.Employees,
  '/users': MODULE_KEYS.Users,
  '/certifications': MODULE_KEYS.Certifications,
  '/training': MODULE_KEYS.Training,
  '/skills': MODULE_KEYS.Skills,
  '/roles': MODULE_KEYS.Roles,
  '/expirations': MODULE_KEYS.Expirations,
  '/reports': MODULE_KEYS.Reports,
  '/settings': MODULE_KEYS.Settings,
  '/my': MODULE_KEYS.MyRequirements,
  '/profile': MODULE_KEYS.Profile,
  '/about': MODULE_KEYS.About,
}

export function moduleForPath(path: string): ModuleKey | null {
  if (path === '/') return MODULE_KEYS.Dashboard
  const match = Object.entries(ROUTE_MODULE).find(([route]) => route !== '/' && path.startsWith(route))
  return match?.[1] ?? null
}

export function useAppSettings() {
  return useQuery({
    queryKey: settingsKeys.app,
    queryFn: () => api<AppSettings>('/api/settings'),
    staleTime: 60_000,
  })
}

export function useMyModules() {
  return useQuery({
    queryKey: settingsKeys.myModules,
    queryFn: () => api<{ modules: ModuleAccess[] }>('/api/settings/modules/me'),
    staleTime: 60_000,
  })
}

export function useModuleAccessMatrix() {
  return useQuery({
    queryKey: settingsKeys.modules,
    queryFn: () => api<{ modules: ModuleAccess[] }>('/api/settings/modules'),
  })
}

export function useUpdateAppSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: AppSettings) =>
      api<AppSettings>('/api/settings', { method: 'PUT', body: JSON.stringify(body) }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.all })
    },
  })
}

export function useUpdateModuleAccess() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: { accessLevel: AccessLevel; moduleKey: string; isEnabled: boolean }) =>
      api<{ modules: ModuleAccess[] }>('/api/settings/modules', {
        method: 'PUT',
        body: JSON.stringify(body),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.all })
    },
  })
}

export interface SkillAssignmentRequest {
  id: number
  employeeId: number
  employeeName: string
  skillId: number
  skillName: string
  requestedProficiency: string
  requestedByName: string
  requestedAt: string
  status: 'Pending' | 'Approved' | 'Rejected'
  notes?: string
  reviewNotes?: string
}

export function usePendingSkillRequests() {
  return useQuery({
    queryKey: ['skill-requests', 'pending'],
    queryFn: () => api<SkillAssignmentRequest[]>('/api/skill-requests/pending'),
  })
}

export function useCreateSkillRequest(employeeId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: { skillId: number; proficiencyLevel: string; notes?: string }) =>
      api<SkillAssignmentRequest>(`/api/employees/${employeeId}/skill-requests`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['skill-requests'] })
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
    },
  })
}

export function useReviewSkillRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      requestId,
      action,
      reviewNotes,
    }: {
      requestId: number
      action: 'approve' | 'reject'
      reviewNotes?: string
    }) =>
      api<SkillAssignmentRequest>(`/api/skill-requests/${requestId}/${action}`, {
        method: 'POST',
        body: JSON.stringify({ reviewNotes }),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['skill-requests'] })
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
    },
  })
}
