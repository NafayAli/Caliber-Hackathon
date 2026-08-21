import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usePersona } from '../contexts/PersonaContext'
import { api } from './client'
import type { RequirementKind } from './dashboard'

export type RenewalRequestStatus = 'Pending' | 'Approved' | 'Declined'

export interface RenewalRequestDto {
  id: number
  employeeId: number
  employeeName: string
  kind: RequirementKind
  assignmentId: number
  requirementName: string
  status: RenewalRequestStatus
  employeeNote: string | null
  reviewerNote: string | null
  requestedAt: string
  reviewedAt: string | null
}

export const renewalKeys = {
  all: ['renewals'] as const,
  pending: (personaId: number | null) => [...renewalKeys.all, 'pending', personaId] as const,
}

export function usePendingRenewals() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: renewalKeys.pending(personaId),
    queryFn: () => api<RenewalRequestDto[]>('/api/renewal-requests/pending'),
    enabled: personaId != null,
  })
}

export function useRequestRenewal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: { kind: RequirementKind; assignmentId: number; note?: string }) =>
      api<RenewalRequestDto>('/api/renewal-requests', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: renewalKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
      void queryClient.invalidateQueries({ queryKey: ['me'] })
    },
  })
}

export function useApproveRenewal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, note }: { id: number; note?: string }) =>
      api<RenewalRequestDto>(`/api/renewal-requests/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ note }),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: renewalKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      void queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })
}

export function useDeclineRenewal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, note }: { id: number; note?: string }) =>
      api<RenewalRequestDto>(`/api/renewal-requests/${id}/decline`, {
        method: 'POST',
        body: JSON.stringify({ note }),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: renewalKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })
}

export function useDirectRenew() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: { kind: RequirementKind; assignmentId: number; renewedOn?: string }) =>
      api<void>('/api/renewals/direct', { method: 'POST', body: JSON.stringify(body) }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: ['employees'] })
      void queryClient.invalidateQueries({ queryKey: ['me'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      void queryClient.invalidateQueries({ queryKey: ['expirations'] })
    },
  })
}
