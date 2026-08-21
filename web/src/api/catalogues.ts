import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usePersona } from '../contexts/PersonaContext'
import { api } from './client'

export type CertificationCategory = 'Oem' | 'Safety' | 'Regulatory' | 'Internal'
export type TrainingCategory = 'Oem' | 'Safety' | 'Onboarding' | 'Product' | 'Internal'
export type DeliveryMode = 'Online' | 'InPerson' | 'OnTheJob' | 'Document'
export type SkillCategory = 'Oem' | 'EquipmentType' | 'SystemType' | 'Safety'
export type ProficiencyLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert'

export interface GrantedSkill {
  skillId: number
  skillName: string
  grantedProficiency: ProficiencyLevel
}

export interface Certification {
  id: number
  name: string
  code: string
  category: CertificationCategory
  issuingBody: string
  description: string | null
  validityMonths: number | null
  expiryWarningDays: number
  requiresEvidence: boolean
  isActive: boolean
  grantedSkills: GrantedSkill[]
}

export interface TrainingProgram {
  id: number
  name: string
  code: string
  category: TrainingCategory
  provider: string
  description: string | null
  deliveryMode: DeliveryMode
  estimatedDurationHours: number
  requiresAcknowledgement: boolean
  recurrenceMonths: number | null
  expiryWarningDays: number
  requiresEvidence: boolean
  isActive: boolean
  grantedSkills: GrantedSkill[]
}

export interface Skill {
  id: number
  name: string
  category: SkillCategory
  description: string | null
  isActive: boolean
}

export interface JobRoleItem {
  id: number
  name: string
  department: string
}

export interface SkillGrantInput {
  skillId: number
  grantedProficiency: ProficiencyLevel
}

export interface CreateCertificationBody {
  name: string
  code: string
  category: CertificationCategory
  issuingBody: string
  description?: string
  validityMonths?: number
  expiryWarningDays?: number
  requiresEvidence: boolean
  grantedSkills?: SkillGrantInput[]
}

export interface UpdateCertificationBody {
  name?: string
  code?: string
  category?: CertificationCategory
  issuingBody?: string
  description?: string
  validityMonths?: number
  expiryWarningDays?: number
  requiresEvidence?: boolean
}

export interface CreateTrainingProgramBody {
  name: string
  code: string
  category: TrainingCategory
  provider: string
  description?: string
  deliveryMode: DeliveryMode
  estimatedDurationHours: number
  requiresAcknowledgement: boolean
  recurrenceMonths?: number
  expiryWarningDays?: number
  requiresEvidence: boolean
  grantedSkills?: SkillGrantInput[]
}

export interface UpdateTrainingProgramBody {
  name?: string
  code?: string
  category?: TrainingCategory
  provider?: string
  description?: string
  deliveryMode?: DeliveryMode
  estimatedDurationHours?: number
  requiresAcknowledgement?: boolean
  recurrenceMonths?: number
  expiryWarningDays?: number
  requiresEvidence?: boolean
}

export interface CreateSkillBody {
  name: string
  category: SkillCategory
  description?: string
}

export interface UpdateSkillBody {
  name?: string
  category?: SkillCategory
  description?: string
}

export const catalogueKeys = {
  all: ['catalogues'] as const,
  certifications: (personaId: number | null) =>
    [...catalogueKeys.all, 'certifications', personaId] as const,
  certification: (personaId: number | null, id: number) =>
    [...catalogueKeys.all, 'certification', personaId, id] as const,
  training: (personaId: number | null) =>
    [...catalogueKeys.all, 'training', personaId] as const,
  trainingProgram: (personaId: number | null, id: number) =>
    [...catalogueKeys.all, 'training-program', personaId, id] as const,
  skills: (personaId: number | null) =>
    [...catalogueKeys.all, 'skills', personaId] as const,
  skill: (personaId: number | null, id: number) =>
    [...catalogueKeys.all, 'skill', personaId, id] as const,
  jobRoles: (personaId: number | null) =>
    [...catalogueKeys.all, 'job-roles', personaId] as const,
}

function invalidateCatalogues(queryClient: ReturnType<typeof useQueryClient>) {
  void queryClient.invalidateQueries({ queryKey: catalogueKeys.all })
}

export function useCertifications() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.certifications(personaId),
    queryFn: () => api<Certification[]>('/api/certifications'),
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

export function useCertification(id: number | null) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.certification(personaId, id ?? 0),
    queryFn: () => api<Certification>(`/api/certifications/${id}`),
    enabled: personaId != null && id != null && id > 0,
  })
}

export function useCreateCertification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateCertificationBody) =>
      api<Certification>('/api/certifications', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useUpdateCertification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...body }: UpdateCertificationBody & { id: number }) =>
      api<Certification>(`/api/certifications/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useDeactivateCertification() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api<void>(`/api/certifications/${id}`, { method: 'DELETE' }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useSetCertificationGrantedSkills(certificationId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (grants: SkillGrantInput[]) =>
      api<Certification>(`/api/certifications/${certificationId}/granted-skills`, {
        method: 'POST',
        body: JSON.stringify({ grants }),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useTrainingPrograms() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.training(personaId),
    queryFn: () => api<TrainingProgram[]>('/api/training-programs'),
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

export function useTrainingProgram(id: number | null) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.trainingProgram(personaId, id ?? 0),
    queryFn: () => api<TrainingProgram>(`/api/training-programs/${id}`),
    enabled: personaId != null && id != null && id > 0,
  })
}

export function useCreateTrainingProgram() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateTrainingProgramBody) =>
      api<TrainingProgram>('/api/training-programs', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useUpdateTrainingProgram() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...body }: UpdateTrainingProgramBody & { id: number }) =>
      api<TrainingProgram>(`/api/training-programs/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useDeactivateTrainingProgram() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api<void>(`/api/training-programs/${id}`, { method: 'DELETE' }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useSetTrainingGrantedSkills(trainingProgramId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (grants: SkillGrantInput[]) =>
      api<TrainingProgram>(`/api/training-programs/${trainingProgramId}/granted-skills`, {
        method: 'POST',
        body: JSON.stringify({ grants }),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useSkillsCatalogue() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.skills(personaId),
    queryFn: () => api<Skill[]>('/api/skills'),
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

export function useSkill(id: number | null) {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.skill(personaId, id ?? 0),
    queryFn: () => api<Skill>(`/api/skills/${id}`),
    enabled: personaId != null && id != null && id > 0,
  })
}

export function useCreateSkill() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateSkillBody) =>
      api<Skill>('/api/skills', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useUpdateSkill() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...body }: UpdateSkillBody & { id: number }) =>
      api<Skill>(`/api/skills/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useDeactivateSkill() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api<void>(`/api/skills/${id}`, { method: 'DELETE' }),
    onSettled: () => invalidateCatalogues(queryClient),
  })
}

export function useJobRoles() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: catalogueKeys.jobRoles(personaId),
    queryFn: () => api<JobRoleItem[]>('/api/job-roles'),
    enabled: personaId != null,
    staleTime: 60_000,
  })
}

/** @deprecated Use useCertifications — kept for assign pickers */
export function useCertificationCatalogue() {
  return useCertifications()
}

/** @deprecated Use useTrainingPrograms */
export function useTrainingCatalogue() {
  return useTrainingPrograms()
}

export type CertificationCatalogueItem = Pick<Certification, 'id' | 'name' | 'code'>
export type TrainingCatalogueItem = Pick<TrainingProgram, 'id' | 'name' | 'code'>
