import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type { AuthUser } from './auth'

export interface UpdateProfileBody {
  firstName?: string
  lastName?: string
  phone?: string
  bio?: string
}

export interface ChangePasswordBody {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: UpdateProfileBody) =>
      api<AuthUser>('/api/me/profile', {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
    },
  })
}

export function useUploadAvatar() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api<AuthUser>('/api/me/avatar', {
        method: 'POST',
        body: form,
      })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
    },
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (body: ChangePasswordBody) =>
      api<void>('/api/auth/change-password', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
  })
}
