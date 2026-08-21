import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usePersona } from '../contexts/PersonaContext'
import { api } from './client'
import type { RequirementKind } from './dashboard'

export type NotificationKind =
  | 'Announcement'
  | 'ExpiryAlert'
  | 'PendingRequirement'
  | 'Acknowledgement'
  | 'RenewalRequest'
  | 'RenewalDecision'
  | 'Reminder'
  | 'System'

export interface NotificationDto {
  id: number
  kind: NotificationKind
  title: string
  message: string
  isRead: boolean
  createdAt: string
  relatedEmployeeId: number | null
  relatedKind: RequirementKind | null
  relatedAssignmentId: number | null
  renewalRequestId: number | null
  createdByName: string | null
}

export interface NotificationSummaryDto {
  unreadCount: number
  items: NotificationDto[]
}

export const notificationKeys = {
  all: ['notifications'] as const,
  summary: (personaId: number | null) => [...notificationKeys.all, 'summary', personaId] as const,
}

export function useNotifications() {
  const { personaId } = usePersona()

  return useQuery({
    queryKey: notificationKeys.summary(personaId),
    queryFn: () => api<NotificationSummaryDto>('/api/notifications'),
    enabled: personaId != null,
    refetchInterval: 60_000,
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api<void>(`/api/notifications/${id}/read`, { method: 'PATCH' }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all })
    },
  })
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => api<void>('/api/notifications/read-all', { method: 'PATCH' }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all })
    },
  })
}

export function useBroadcastAnnouncement() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: { title: string; message: string; locationId?: number }) =>
      api<{ sent: number }>('/api/notifications/broadcast', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all })
    },
  })
}

export function useNotifyEmployees() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: {
      employeeIds: number[]
      title: string
      message: string
      kind?: NotificationKind
    }) =>
      api<{ sent: number }>('/api/notifications/notify-employees', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all })
    },
  })
}
