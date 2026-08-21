import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, apiPublic, ApiError } from './client'
import { setAuthSessionUser } from '../lib/authSession'
import { clearPersonaId } from './persona'
import type { AccessLevel } from './types'

export interface AuthUser {
  employeeId: number
  email: string
  firstName: string
  lastName: string
  displayName: string
  accessLevel: AccessLevel
  locationId: number
  locationName: string
  jobRoleName: string
  phone?: string
  bio?: string
  avatarUrl?: string
}

export interface LoginBody {
  email: string
  password: string
}

export interface RegisterBody {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  locationId: number
  jobRoleId: number
}

export interface LocationOption {
  id: number
  name: string
  code: string
  city: string
}

export function useAuthMe(enabled = true) {
  return useQuery({
    queryKey: ['auth', 'me'],
    queryFn: async () => {
      try {
        return await api<AuthUser>('/api/auth/me')
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          return null
        }
        throw error
      }
    },
    retry: false,
    enabled,
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnMount: true,
  })
}

export function useLocations() {
  return useQuery({
    queryKey: ['locations'],
    queryFn: () => apiPublic<LocationOption[]>('/api/locations'),
  })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: LoginBody) =>
      api<AuthUser>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
    onSuccess: (user) => {
      clearPersonaId()
      setAuthSessionUser(user)
      queryClient.setQueryData(['auth', 'me'], user)
    },
  })
}

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: RegisterBody) =>
      apiPublic<AuthUser>('/api/auth/register', { method: 'POST', body: JSON.stringify(body) }),
    onSuccess: (user) => {
      clearPersonaId()
      setAuthSessionUser(user)
      queryClient.setQueryData(['auth', 'me'], user)
    },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api<void>('/api/auth/logout', { method: 'POST' }),
    onMutate: () => {
      clearPersonaId()
      setAuthSessionUser(null)
      queryClient.setQueryData(['auth', 'me'], null)
    },
    onSuccess: () => {
      queryClient.clear()
    },
    onError: () => {
      queryClient.clear()
    },
  })
}
