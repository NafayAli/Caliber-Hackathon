import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usePersona } from '../contexts/PersonaContext'
import { api } from './client'
import type { RequirementKind } from './dashboard'
import type { ProficiencyLevel } from './catalogues'

export interface RoleRequirement {
  id: number
  kind: RequirementKind
  certificationId: number | null
  trainingProgramId: number | null
  skillId: number | null
  name: string
  minimumProficiency: ProficiencyLevel | null
  isMandatory: boolean
  dueWithinDaysOfHire: number | null
}

export interface JobRole {
  id: number
  name: string
  department: string
  departmentId: number
  requirements: RoleRequirement[]
}

export interface ApplyRoleResult {
  certificationsCreated: number
  trainingsCreated: number
}

export interface CreateJobRoleBody {
  name: string
  departmentId: number
}

export interface UpdateJobRoleBody {
  name?: string
  departmentId?: number
}

export interface DepartmentOption {
  id: number
  name: string
}

export interface AddRoleRequirementBody {
  kind: RequirementKind
  certificationId?: number
  trainingProgramId?: number
  skillId?: number
  minimumProficiency?: ProficiencyLevel
  isMandatory?: boolean
  dueWithinDaysOfHire?: number
}

export const roleKeys = {
  all: ['roles'] as const,
  list: (personaId: number | null) => [...roleKeys.all, 'list', personaId] as const,
  detail: (personaId: number | null, id: number) =>
    [...roleKeys.all, 'detail', personaId, id] as const,
}

export function useJobRolesWithRequirements() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: roleKeys.list(personaId),
    queryFn: () => api<JobRole[]>('/api/job-roles'),
    enabled: personaId != null,
    staleTime: 30_000,
  })
}

export function useJobRole(id: number | null) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: roleKeys.detail(personaId, id ?? 0),
    queryFn: () => api<JobRole>(`/api/job-roles/${id}`),
    enabled: personaId != null && id != null && id > 0,
  })
}

export function useApplyRoleRequirements(roleId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () =>
      api<ApplyRoleResult>(`/api/job-roles/${roleId}/apply`, { method: 'POST' }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
    },
  })
}

export function useAddRoleRequirement(roleId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: AddRoleRequirementBody) =>
      api<RoleRequirement>(`/api/job-roles/${roleId}/requirements`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
  })
}

export function useDepartments() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: ['departments', personaId],
    queryFn: () => api<DepartmentOption[]>('/api/departments'),
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

export function useCreateJobRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateJobRoleBody) =>
      api<JobRole>('/api/job-roles', { method: 'POST', body: JSON.stringify(body) }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
  })
}

export function useUpdateJobRole(roleId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: UpdateJobRoleBody) =>
      api<JobRole>(`/api/job-roles/${roleId}`, { method: 'PATCH', body: JSON.stringify(body) }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
  })
}

export function useDeleteJobRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (roleId: number) =>
      api<void>(`/api/job-roles/${roleId}`, { method: 'DELETE' }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
  })
}
