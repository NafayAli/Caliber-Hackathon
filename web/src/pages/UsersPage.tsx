import { Plus, Search } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { useLocations } from '../api/auth'
import { getApiErrorMessage } from '../api/client'
import { useJobRoles } from '../api/catalogues'
import {
  useCreateUser,
  useUpdateUser,
  useUsers,
  type UserListItem,
} from '../api/users'
import type { AccessLevel } from '../api/types'
import { Field, fieldClassName } from '../components/catalogue/formFields'
import { Avatar } from '../components/ios/Avatar'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { Sheet } from '../components/ios/Sheet'
import { isAdmin, useAuth } from '../contexts/AuthContext'
import { cn } from '../lib/cn'

const ACCESS_LEVELS: AccessLevel[] = ['Technician', 'Manager', 'Admin']

interface CreateForm {
  firstName: string
  lastName: string
  email: string
  password: string
  jobRoleId: number | ''
  locationId: number | ''
  accessLevel: AccessLevel
}

const defaultCreateForm = (): CreateForm => ({
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  jobRoleId: '',
  locationId: '',
  accessLevel: 'Technician',
})

export function UsersPage() {
  const { user, accessLevel } = useAuth()
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const [editUser, setEditUser] = useState<UserListItem | null>(null)
  const [createForm, setCreateForm] = useState(defaultCreateForm)

  const query = useMemo(
    () => ({
      search: search || undefined,
      limit: 100,
      locationId: !isAdmin(accessLevel) && user ? user.locationId : undefined,
    }),
    [search, accessLevel, user],
  )

  const { data, isLoading, isError } = useUsers(query)
  const { data: jobRoles = [] } = useJobRoles()
  const { data: locations = [] } = useLocations()
  const createUser = useCreateUser()
  const updateUser = useUpdateUser()

  const availableLocations = useMemo(() => {
    if (isAdmin(accessLevel)) return locations
    if (!user) return []
    return locations.filter((loc) => loc.id === user.locationId)
  }, [accessLevel, locations, user])

  async function submitCreate() {
    if (
      !createForm.firstName.trim()
      || !createForm.lastName.trim()
      || !createForm.email.trim()
      || !createForm.password
      || createForm.jobRoleId === ''
      || createForm.locationId === ''
    ) {
      return
    }

    try {
      await createUser.mutateAsync({
        firstName: createForm.firstName.trim(),
        lastName: createForm.lastName.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
        jobRoleId: createForm.jobRoleId,
        locationId: createForm.locationId,
        accessLevel: isAdmin(accessLevel) ? createForm.accessLevel : 'Technician',
      })
      toast.success('User created')
      setCreating(false)
      setCreateForm(defaultCreateForm())
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Users"
        subtitle="Manage team accounts"
        actions={
          <button
            type="button"
            onClick={() => {
              setCreateForm({
                ...defaultCreateForm(),
                locationId: !isAdmin(accessLevel) && user ? user.locationId : '',
              })
              setCreating(true)
            }}
            className="inline-flex min-h-10 items-center gap-1 rounded-xl bg-accent px-4 text-sm font-semibold text-white"
          >
            <Plus className="h-4 w-4" />
            Add user
          </button>
        }
      />

      <div className="relative mb-4">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-label" />
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search by name or email"
          className={cn(fieldClassName, 'pl-10')}
        />
      </div>

      {isLoading ? (
        <InsetGroupedList>
          {Array.from({ length: 5 }).map((_, index) => (
            <div key={index} className="animate-pulse border-b border-separator/60 px-4 py-3">
              <div className="h-4 w-2/5 rounded bg-separator/50" />
              <div className="mt-2 h-3 w-3/5 rounded bg-separator/40" />
            </div>
          ))}
        </InsetGroupedList>
      ) : null}

      {isError ? (
        <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
          <p className="font-medium text-label">Unable to load users</p>
        </div>
      ) : null}

      {data ? (
        <InsetGroupedList>
          {data.items.length === 0 ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No users match your search.
            </div>
          ) : (
            data.items.map((item) => (
              <Row
                key={item.id}
                label={
                  <div className="flex items-center gap-3">
                    <Avatar name={item.fullName} size="md" />
                    <div className="min-w-0">
                      <div className="truncate font-medium">{item.fullName}</div>
                      <div className="truncate text-sm text-secondary-label">{item.email}</div>
                    </div>
                  </div>
                }
                description={`${item.jobRole} · ${item.location}`}
                chevron
                onClick={() => setEditUser(item)}
              />
            ))
          )}
        </InsetGroupedList>
      ) : null}

      <Sheet open={creating} onOpenChange={setCreating} title="New user">
        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="First name">
              <input
                value={createForm.firstName}
                onChange={(event) =>
                  setCreateForm({ ...createForm, firstName: event.target.value })
                }
                className={fieldClassName}
              />
            </Field>
            <Field label="Last name">
              <input
                value={createForm.lastName}
                onChange={(event) =>
                  setCreateForm({ ...createForm, lastName: event.target.value })
                }
                className={fieldClassName}
              />
            </Field>
          </div>
          <Field label="Email">
            <input
              type="email"
              value={createForm.email}
              onChange={(event) => setCreateForm({ ...createForm, email: event.target.value })}
              className={fieldClassName}
            />
          </Field>
          <Field label="Initial password">
            <input
              type="password"
              value={createForm.password}
              onChange={(event) => setCreateForm({ ...createForm, password: event.target.value })}
              className={fieldClassName}
              autoComplete="new-password"
            />
          </Field>
          <Field label="Job role">
            <select
              value={createForm.jobRoleId}
              onChange={(event) =>
                setCreateForm({
                  ...createForm,
                  jobRoleId: event.target.value ? Number(event.target.value) : '',
                })
              }
              className={fieldClassName}
            >
              <option value="">Select role</option>
              {jobRoles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Location">
            <select
              value={createForm.locationId}
              onChange={(event) =>
                setCreateForm({
                  ...createForm,
                  locationId: event.target.value ? Number(event.target.value) : '',
                })
              }
              className={fieldClassName}
              disabled={!isAdmin(accessLevel)}
            >
              <option value="">Select location</option>
              {availableLocations.map((location) => (
                <option key={location.id} value={location.id}>
                  {location.name}
                </option>
              ))}
            </select>
          </Field>
          {isAdmin(accessLevel) ? (
            <Field label="Access level">
              <select
                value={createForm.accessLevel}
                onChange={(event) =>
                  setCreateForm({
                    ...createForm,
                    accessLevel: event.target.value as AccessLevel,
                  })
                }
                className={fieldClassName}
              >
                {ACCESS_LEVELS.map((level) => (
                  <option key={level} value={level}>
                    {level}
                  </option>
                ))}
              </select>
            </Field>
          ) : null}
          <button
            type="button"
            disabled={
              !createForm.firstName.trim()
              || !createForm.lastName.trim()
              || !createForm.email.trim()
              || !createForm.password
              || createForm.jobRoleId === ''
              || createForm.locationId === ''
              || createUser.isPending
            }
            onClick={() => void submitCreate()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Create user
          </button>
        </div>
      </Sheet>

      <Sheet
        open={editUser != null}
        onOpenChange={(open) => {
          if (!open) setEditUser(null)
        }}
        title={editUser?.fullName ?? 'Edit user'}
      >
        {editUser ? (
          <EditUserForm
            user={editUser}
            jobRoles={jobRoles}
            locations={availableLocations}
            canSetAccessLevel={isAdmin(accessLevel)}
            onSave={async (body) => {
              try {
                await updateUser.mutateAsync({ id: editUser.id, ...body })
                toast.success('User updated')
                setEditUser(null)
              } catch (error) {
                toast.error(getApiErrorMessage(error))
              }
            }}
            isPending={updateUser.isPending}
          />
        ) : null}
      </Sheet>
    </div>
  )
}

function EditUserForm({
  user,
  jobRoles,
  locations,
  canSetAccessLevel,
  onSave,
  isPending,
}: {
  user: UserListItem
  jobRoles: Array<{ id: number; name: string }>
  locations: Array<{ id: number; name: string }>
  canSetAccessLevel: boolean
  onSave: (body: {
    firstName: string
    lastName: string
    email: string
    jobRoleId: number
    locationId: number
    accessLevel?: AccessLevel
  }) => Promise<void>
  isPending: boolean
}) {
  const [firstName, setFirstName] = useState(user.fullName.split(' ')[0] ?? '')
  const [lastName, setLastName] = useState(user.fullName.split(' ').slice(1).join(' '))
  const [email, setEmail] = useState(user.email)
  const [jobRoleId, setJobRoleId] = useState<number | ''>('')
  const [locationId, setLocationId] = useState<number | ''>('')
  const [accessLevel, setAccessLevelValue] = useState<AccessLevel>('Technician')

  useEffect(() => {
    const role = jobRoles.find((r) => r.name === user.jobRole)
    const location = locations.find((l) => l.name === user.location)
    if (role) setJobRoleId(role.id)
    if (location) setLocationId(location.id)
    setFirstName(user.fullName.split(' ')[0] ?? '')
    setLastName(user.fullName.split(' ').slice(1).join(' '))
    setEmail(user.email)
  }, [user, jobRoles, locations])

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="First name">
          <input value={firstName} onChange={(e) => setFirstName(e.target.value)} className={fieldClassName} />
        </Field>
        <Field label="Last name">
          <input value={lastName} onChange={(e) => setLastName(e.target.value)} className={fieldClassName} />
        </Field>
      </div>
      <Field label="Email">
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={fieldClassName}
        />
      </Field>
      <Field label="Job role">
        <select
          value={jobRoleId}
          onChange={(e) => setJobRoleId(e.target.value ? Number(e.target.value) : '')}
          className={fieldClassName}
        >
          <option value="">Select role</option>
          {jobRoles.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Location">
        <select
          value={locationId}
          onChange={(e) => setLocationId(e.target.value ? Number(e.target.value) : '')}
          className={fieldClassName}
          disabled={!canSetAccessLevel && locations.length <= 1}
        >
          <option value="">Select location</option>
          {locations.map((location) => (
            <option key={location.id} value={location.id}>
              {location.name}
            </option>
          ))}
        </select>
      </Field>
      {canSetAccessLevel ? (
        <Field label="Access level">
          <select
            value={accessLevel}
            onChange={(e) => setAccessLevelValue(e.target.value as AccessLevel)}
            className={fieldClassName}
          >
            {ACCESS_LEVELS.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
        </Field>
      ) : null}
      <button
        type="button"
        disabled={
          !firstName.trim()
          || !lastName.trim()
          || !email.trim()
          || jobRoleId === ''
          || locationId === ''
          || isPending
        }
        onClick={() =>
          void onSave({
            firstName: firstName.trim(),
            lastName: lastName.trim(),
            email: email.trim(),
            jobRoleId: Number(jobRoleId),
            locationId: Number(locationId),
            accessLevel: canSetAccessLevel ? accessLevel : undefined,
          })
        }
        className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
      >
        Save changes
      </button>
    </div>
  )
}
