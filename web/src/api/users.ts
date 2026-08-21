import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type { AccessLevel } from './types'
import type { EmployeeProfile } from './employees'
import { employeeKeys } from './employees'

export interface CreateUserBody {
  firstName: string
  lastName: string
  email: string
  password: string
  jobRoleId: number
  locationId: number
  externalEmployeeNo?: string
  hireDate?: string
  accessLevel?: AccessLevel
}

export interface UpdateUserBody {
  firstName?: string
  lastName?: string
  email?: string
  jobRoleId?: number
  locationId?: number
  externalEmployeeNo?: string
  hireDate?: string
  accessLevel?: AccessLevel
  isActive?: boolean
}

export { useEmployees as useUsers, fetchEmployees as fetchUsers } from './employees'
export type { EmployeeListItem as UserListItem, EmployeeListQuery as UserListQuery } from './employees'

export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateUserBody) =>
      api<EmployeeProfile>('/api/employees', {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...body }: UpdateUserBody & { id: number }) =>
      api<EmployeeProfile>(`/api/employees/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(body),
      }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}
