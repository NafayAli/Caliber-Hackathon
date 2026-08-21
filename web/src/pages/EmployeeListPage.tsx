import { useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDashboard } from '../api/dashboard'
import {
  prefetchEmployee,
  useEmployees,
  type EmployeeListQuery,
} from '../api/employees'
import { useJobRoles } from '../api/catalogues'
import { Avatar } from '../components/ios/Avatar'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { ReadinessBar, StatusChip, type ReadinessStatus } from '../components/StatusChip'
import { usePersona } from '../contexts/PersonaContext'
import { cn } from '../lib/cn'

const STATUS_FILTERS: Array<{ value: ReadinessStatus | ''; label: string }> = [
  { value: '', label: 'All statuses' },
  { value: 'Compliant', label: 'Compliant' },
  { value: 'ExpiringSoon', label: 'Expiring soon' },
  { value: 'Expired', label: 'Expired' },
  { value: 'Overdue', label: 'Overdue' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'Missing', label: 'Missing' },
  { value: 'Waived', label: 'Waived' },
]

function ListSkeleton() {
  return (
    <InsetGroupedList>
      {Array.from({ length: 6 }).map((_, index) => (
        <div
          key={index}
          className="flex animate-pulse items-center gap-3 border-b border-separator/60 px-4 py-3 last:border-b-0"
        >
          <div className="h-10 w-10 rounded-full bg-separator/50" />
          <div className="min-w-0 flex-1 space-y-2">
            <div className="h-4 w-2/5 rounded bg-separator/50" />
            <div className="h-3 w-3/5 rounded bg-separator/40" />
            <div className="h-2 w-full rounded-full bg-separator/30" />
          </div>
        </div>
      ))}
    </InsetGroupedList>
  )
}

export function EmployeeListPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { personaId } = usePersona()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<ReadinessStatus | ''>('')
  const [jobRoleId, setJobRoleId] = useState<number | ''>('')
  const [locationId, setLocationId] = useState<number | ''>('')

  const query = useMemo<EmployeeListQuery>(
    () => ({
      search: search || undefined,
      status: status || undefined,
      jobRoleId: jobRoleId === '' ? undefined : jobRoleId,
      locationId: locationId === '' ? undefined : locationId,
      limit: 50,
    }),
    [search, status, jobRoleId, locationId],
  )

  const { data, isLoading, isError } = useEmployees(query)
  const { data: jobRoles = [] } = useJobRoles()
  const { data: dashboard } = useDashboard()

  const locations = dashboard?.byLocation ?? []

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Employees"
        subtitle="Readiness across your crew"
      />

      <div className="mb-4 space-y-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-label" />
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search by name or email"
            className={cn(
              'min-h-11 w-full rounded-xl bg-grouped pl-10 pr-4 text-[15px] text-label',
              'ring-1 ring-separator/50 placeholder:text-secondary-label focus:outline-none focus:ring-accent',
            )}
          />
        </div>

        <div className="flex flex-wrap gap-2">
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value as ReadinessStatus | '')}
            className="min-h-9 rounded-lg bg-elevated px-3 text-sm text-label ring-1 ring-separator/50"
          >
            {STATUS_FILTERS.map((option) => (
              <option key={option.label} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>

          <select
            value={jobRoleId}
            onChange={(event) =>
              setJobRoleId(event.target.value ? Number(event.target.value) : '')
            }
            className="min-h-9 rounded-lg bg-elevated px-3 text-sm text-label ring-1 ring-separator/50"
          >
            <option value="">All roles</option>
            {jobRoles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>

          {locations.length > 1 ? (
            <select
              value={locationId}
              onChange={(event) =>
                setLocationId(event.target.value ? Number(event.target.value) : '')
              }
              className="min-h-9 rounded-lg bg-elevated px-3 text-sm text-label ring-1 ring-separator/50"
            >
              <option value="">All locations</option>
              {locations.map((location) => (
                <option key={location.locationId} value={location.locationId}>
                  {location.locationName}
                </option>
              ))}
            </select>
          ) : null}
        </div>
      </div>

      {isLoading ? <ListSkeleton /> : null}

      {isError ? (
        <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
          <p className="font-medium text-label">Unable to load employees</p>
        </div>
      ) : null}

      {data ? (
        <>
          <InsetGroupedList>
            {data.items.length === 0 ? (
              <div className="px-4 py-8 text-center text-sm text-secondary-label">
                No employees match your filters.
              </div>
            ) : (
              data.items.map((employee) => (
                <Row
                  key={employee.id}
                  label={
                    <div className="flex items-center gap-3">
                      <Avatar name={employee.fullName} size="md" />
                      <div className="min-w-0">
                        <div className="truncate font-medium">{employee.fullName}</div>
                        <div className="truncate text-sm text-secondary-label">
                          {employee.jobRole} · {employee.location}
                        </div>
                      </div>
                    </div>
                  }
                  description={
                    <div className="mt-2 space-y-1.5 pl-[52px]">
                      <ReadinessBar percent={employee.readinessPercent} />
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-xs tabular-nums text-secondary-label">
                          {employee.readinessPercent}% ready
                        </span>
                        <StatusChip status={employee.worstStatus} />
                      </div>
                    </div>
                  }
                  chevron
                  onClick={() => navigate(`/employees/${employee.id}`)}
                  onMouseEnter={() => prefetchEmployee(queryClient, personaId, employee.id)}
                />
              ))
            )}
          </InsetGroupedList>

          {data.totalCount > data.items.length ? (
            <p className="mt-3 px-4 text-xs text-secondary-label">
              Showing {data.items.length} of {data.totalCount} employees
            </p>
          ) : null}
        </>
      ) : null}
    </div>
  )
}
