import type { QueryClient } from '@tanstack/react-query'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ReadinessStatus } from '../components/StatusChip'
import { usePersona } from '../contexts/PersonaContext'
import { api } from './client'
import type { RequirementKind } from './dashboard'

export type AssignmentStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Waived'

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  offset: number
  limit: number
}

export interface EmployeeListQuery {
  offset?: number
  limit?: number
  locationId?: number
  jobRoleId?: number
  status?: ReadinessStatus
  search?: string
}

export interface EmployeeListItem {
  id: number
  fullName: string
  email: string
  jobRole: string
  location: string
  readinessPercent: number
  worstStatus: ReadinessStatus
}

export interface RequirementStatus {
  kind: RequirementKind
  sourceId: number
  catalogueId: number
  name: string
  category: string
  assignmentStatus: AssignmentStatus
  completedOn: string | null
  effectiveDate: string | null
  dueOn: string | null
  warningDays: number
  status: ReadinessStatus
  isMandatory: boolean
  rowVersion: string
  requiresAcknowledgement: boolean
  acknowledgedOn: string | null
  pendingRenewalRequestId: number | null
}

export type SkillCategory = 'Oem' | 'EquipmentType' | 'SystemType' | 'Safety'
export type ProficiencyLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert'
export type SkillSourceType =
  | 'Certification'
  | 'Training'
  | 'Experience'
  | 'ManagerAssessed'

export interface EmployeeSkill {
  id: number
  skillId: number
  skillName: string
  category: SkillCategory
  proficiencyLevel: ProficiencyLevel
  sourceType: SkillSourceType
  sourceCertificationId: number | null
  sourceTrainingProgramId: number | null
  assessedOn: string
  assessedBy: string
  notes: string | null
}

export type EvidenceType = 'Certificate' | 'Acknowledgement' | 'Scan' | 'Photo' | 'Other' | 'General'

export interface EvidenceItem {
  id: number
  employeeId: number
  evidenceType: EvidenceType
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedOn: string
  uploadedBy: string
  isVerified: boolean
  verifiedBy: string | null
  verifiedOn: string | null
  employeeCertificationId: number | null
  employeeTrainingId: number | null
  employeeSkillId: number | null
}

export interface EmployeeProfile {
  id: number
  firstName: string
  lastName: string
  fullName: string
  email: string
  externalEmployeeNo: string | null
  jobRole: string
  jobRoleId: number
  location: string
  locationId: number
  hireDate: string
  accessLevel: 'Technician' | 'Manager' | 'Admin'
  readinessPercent: number
  requirements: RequirementStatus[]
  skills: EmployeeSkill[]
  evidence: EvidenceItem[]
}

function buildEmployeeListParams(query: EmployeeListQuery): string {
  const params = new URLSearchParams()
  params.set('offset', String(query.offset ?? 0))
  params.set('limit', String(query.limit ?? 50))
  if (query.locationId != null) params.set('locationId', String(query.locationId))
  if (query.jobRoleId != null) params.set('jobRoleId', String(query.jobRoleId))
  if (query.status) params.set('status', query.status)
  if (query.search?.trim()) params.set('search', query.search.trim())
  return params.toString()
}

export const employeeKeys = {
  all: ['employees'] as const,
  list: (personaId: number | null, query: EmployeeListQuery) =>
    [...employeeKeys.all, 'list', personaId, query] as const,
  profile: (personaId: number | null, id: number) =>
    [...employeeKeys.all, 'profile', personaId, id] as const,
}

export function fetchEmployees(query: EmployeeListQuery): Promise<PagedResult<EmployeeListItem>> {
  return api<PagedResult<EmployeeListItem>>(`/api/employees?${buildEmployeeListParams(query)}`)
}

export function fetchEmployeeProfile(id: number): Promise<EmployeeProfile> {
  return api<EmployeeProfile>(`/api/employees/${id}`)
}

export function useEmployees(query: EmployeeListQuery) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: employeeKeys.list(personaId, query),
    queryFn: () => fetchEmployees(query),
    enabled: personaId != null,
  })
}

export function useEmployee(id: number) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: employeeKeys.profile(personaId, id),
    queryFn: () => fetchEmployeeProfile(id),
    enabled: personaId != null && id > 0,
  })
}

export function prefetchEmployee(queryClient: QueryClient, personaId: number | null, id: number) {
  if (personaId == null || id <= 0) return
  void queryClient.prefetchQuery({
    queryKey: employeeKeys.profile(personaId, id),
    queryFn: () => fetchEmployeeProfile(id),
  })
}

function invalidateEmployeeQueries(queryClient: QueryClient, _employeeId: number) {
  void queryClient.invalidateQueries({ queryKey: employeeKeys.all })
  void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
  void queryClient.invalidateQueries({ queryKey: ['me'] })
}

export function useAssignCertification(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: (body: { certificationId: number; dueOn?: string; notes?: string }) =>
      api(`/api/employees/${employeeId}/certifications`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onMutate: async (body) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: [
            ...previous.requirements,
            {
              kind: 'Certification',
              sourceId: -Date.now(),
              catalogueId: body.certificationId,
              name: 'Assigning…',
              category: '',
              assignmentStatus: 'NotStarted',
              completedOn: null,
              effectiveDate: null,
              dueOn: body.dueOn ?? null,
              warningDays: 60,
              status: 'Missing',
              isMandatory: true,
              rowVersion: '',
              requiresAcknowledgement: false,
              acknowledgedOn: null,
              pendingRenewalRequestId: null,
            },
          ],
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useAssignSkill(employeeId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: {
      skillId: number
      proficiencyLevel: ProficiencyLevel
      assessedOn?: string
      notes?: string
    }) =>
      api<EmployeeSkill>(`/api/employees/${employeeId}/skills`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useAssignTraining(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: (body: { trainingProgramId: number; dueOn?: string; notes?: string }) =>
      api(`/api/employees/${employeeId}/training`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onMutate: async (body) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: [
            ...previous.requirements,
            {
              kind: 'Training',
              sourceId: -Date.now(),
              catalogueId: body.trainingProgramId,
              name: 'Assigning…',
              category: '',
              assignmentStatus: 'NotStarted',
              completedOn: null,
              effectiveDate: null,
              dueOn: body.dueOn ?? null,
              warningDays: 60,
              status: 'Missing',
              isMandatory: true,
              rowVersion: '',
              requiresAcknowledgement: false,
              acknowledgedOn: null,
              pendingRenewalRequestId: null,
            },
          ],
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useRecordAward(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: ({
      assignmentId,
      ...body
    }: {
      assignmentId: number
      awardedOn: string
      certificateNumber?: string
      notes?: string
      rowVersion: string
    }) =>
      api(`/api/employee-certifications/${assignmentId}/awards`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onMutate: async ({ assignmentId, awardedOn }) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: previous.requirements.map((req) =>
            req.sourceId === assignmentId
              ? {
                  ...req,
                  status: 'Compliant' as ReadinessStatus,
                  assignmentStatus: 'Completed' as AssignmentStatus,
                  completedOn: awardedOn,
                }
              : req,
          ),
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useWaiveRequirement(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: ({
      assignmentId,
      ...body
    }: {
      assignmentId: number
      reason: string
      rowVersion: string
    }) =>
      api(`/api/employee-certifications/${assignmentId}/waive`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onMutate: async ({ assignmentId }) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: previous.requirements.map((req) =>
            req.sourceId === assignmentId
              ? {
                  ...req,
                  status: 'Waived' as ReadinessStatus,
                  assignmentStatus: 'Waived' as AssignmentStatus,
                }
              : req,
          ),
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useCompleteTraining(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: ({
      assignmentId,
      ...body
    }: {
      assignmentId: number
      completedOn?: string
      score?: number
      notes?: string
      rowVersion: string
    }) =>
      api(`/api/employee-trainings/${assignmentId}/complete`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onMutate: async ({ assignmentId, completedOn }) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      const today = completedOn ?? new Date().toISOString().slice(0, 10)
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: previous.requirements.map((req) =>
            req.sourceId === assignmentId
              ? {
                  ...req,
                  status: 'Compliant' as ReadinessStatus,
                  assignmentStatus: 'Completed' as AssignmentStatus,
                  completedOn: today,
                }
              : req,
          ),
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useAcknowledgeTraining(employeeId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      assignmentId,
      rowVersion,
    }: {
      assignmentId: number
      rowVersion: string
    }) =>
      api(`/api/employee-trainings/${assignmentId}/acknowledge`, {
        method: 'POST',
        body: JSON.stringify({ rowVersion }),
      }),
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useUpdateTrainingProgress(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: ({
      assignmentId,
      ...body
    }: {
      assignmentId: number
      status?: AssignmentStatus
      percentComplete?: number
      startedOn?: string
      notes?: string
      rowVersion: string
    }) =>
      api(`/api/employee-trainings/${assignmentId}`, {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onMutate: async ({ assignmentId }) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          requirements: previous.requirements.map((req) =>
            req.sourceId === assignmentId
              ? {
                  ...req,
                  status: 'InProgress' as ReadinessStatus,
                  assignmentStatus: 'InProgress' as AssignmentStatus,
                }
              : req,
          ),
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useVerifyEvidence(employeeId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ evidenceId, notes }: { evidenceId: number; notes?: string }) =>
      api<EvidenceItem>(`/api/evidence/${evidenceId}/verify`, {
        method: 'POST',
        body: JSON.stringify({ notes }),
      }),
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}

export function useDeleteEvidence(employeeId: number) {
  const queryClient = useQueryClient()
  const { personaId } = usePersona()

  return useMutation({
    mutationFn: (evidenceId: number) =>
      api(`/api/evidence/${evidenceId}`, { method: 'DELETE' }),
    onMutate: async (evidenceId) => {
      await queryClient.cancelQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
      const previous = queryClient.getQueryData<EmployeeProfile>(
        employeeKeys.profile(personaId, employeeId),
      )
      if (previous) {
        queryClient.setQueryData<EmployeeProfile>(employeeKeys.profile(personaId, employeeId), {
          ...previous,
          evidence: previous.evidence.filter((item) => item.id !== evidenceId),
        })
      }
      return { previous }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          employeeKeys.profile(personaId, employeeId),
          context.previous,
        )
      }
    },
    onSettled: () => invalidateEmployeeQueries(queryClient, employeeId),
  })
}
