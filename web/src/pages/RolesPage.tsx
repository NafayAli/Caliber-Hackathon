import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import type { RequirementKind } from '../api/dashboard'
import { getApiErrorMessage } from '../api/client'
import {
  useAddRoleRequirement,
  useApplyRoleRequirements,
  useCreateJobRole,
  useDeleteJobRole,
  useDepartments,
  useJobRole,
  useJobRolesWithRequirements,
  useUpdateJobRole,
  type RoleRequirement,
} from '../api/roles'
import {
  useCertifications,
  useTrainingPrograms,
} from '../api/catalogues'
import { DetailRow, Field, fieldClassName } from '../components/catalogue/formFields'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { Sheet } from '../components/ios/Sheet'

const KINDS: RequirementKind[] = ['Certification', 'Training']

export function RolesPage() {
  const { data: roles = [], isLoading, isError } = useJobRolesWithRequirements()
  const { data: departments = [] } = useDepartments()
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [addingRequirement, setAddingRequirement] = useState(false)
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState(false)

  const { data: roleDetail } = useJobRole(selectedId)
  const apply = useApplyRoleRequirements(selectedId ?? 0)
  const addRequirement = useAddRoleRequirement(selectedId ?? 0)
  const createRole = useCreateJobRole()
  const updateRole = useUpdateJobRole(selectedId ?? 0)
  const deleteRole = useDeleteJobRole()

  const certifications = useCertifications()
  const trainingPrograms = useTrainingPrograms()

  const [kind, setKind] = useState<RequirementKind>('Certification')
  const [catalogueId, setCatalogueId] = useState<number | ''>('')
  const [isMandatory, setIsMandatory] = useState(true)
  const [dueWithinDays, setDueWithinDays] = useState('')

  const [roleName, setRoleName] = useState('')
  const [departmentId, setDepartmentId] = useState<number | ''>('')

  const role = roleDetail ?? roles.find((item) => item.id === selectedId)

  function openCreate() {
    setRoleName('')
    setDepartmentId(departments[0]?.id ?? '')
    setCreating(true)
    setSelectedId(null)
    setEditing(false)
  }

  function openEdit() {
    if (!role) return
    setRoleName(role.name)
    setDepartmentId(role.departmentId)
    setEditing(true)
  }

  async function handleApply() {
    if (!selectedId) return
    try {
      const result = await apply.mutateAsync()
      const total = result.certificationsCreated + result.trainingsCreated
      toast.success(
        total === 0
          ? 'All employees already have these assignments'
          : `Created ${result.certificationsCreated} certification(s) and ${result.trainingsCreated} training assignment(s)`,
      )
    } catch {
      // global handler
    }
  }

  async function handleAddRequirement() {
    if (!selectedId || catalogueId === '') return
    try {
      const body =
        kind === 'Certification'
          ? {
              kind,
              certificationId: catalogueId,
              isMandatory,
              dueWithinDaysOfHire: dueWithinDays ? Number(dueWithinDays) : undefined,
            }
          : {
              kind,
              trainingProgramId: catalogueId,
              isMandatory,
              dueWithinDaysOfHire: dueWithinDays ? Number(dueWithinDays) : undefined,
            }

      await addRequirement.mutateAsync(body)
      toast.success('Requirement added to role template')
      setAddingRequirement(false)
      setCatalogueId('')
    } catch {
      // global handler
    }
  }

  async function handleCreateRole() {
    if (!roleName.trim() || departmentId === '') return
    try {
      const created = await createRole.mutateAsync({
        name: roleName.trim(),
        departmentId,
      })
      toast.success('Job role created')
      setCreating(false)
      setSelectedId(created.id)
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleUpdateRole() {
    if (!selectedId) return
    try {
      await updateRole.mutateAsync({
        name: roleName.trim() || undefined,
        departmentId: departmentId === '' ? undefined : departmentId,
      })
      toast.success('Job role updated')
      setEditing(false)
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleDeleteRole() {
    if (!selectedId || !role) return
    if (!window.confirm(`Delete job role "${role.name}"? This cannot be undone.`)) return
    try {
      await deleteRole.mutateAsync(selectedId)
      toast.success('Job role deleted')
      setSelectedId(null)
      setEditing(false)
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Roles"
        subtitle="Requirement templates by job role"
        actions={
          <button
            type="button"
            onClick={openCreate}
            className="inline-flex min-h-10 items-center gap-1 rounded-xl bg-accent px-4 text-sm font-semibold text-white"
          >
            <Plus className="h-4 w-4" />
            Add role
          </button>
        }
      />

      {isLoading ? <ListSkeleton /> : null}
      {isError ? <ErrorCard message="Unable to load roles" /> : null}

      {!isLoading && !isError ? (
        <InsetGroupedList>
          {roles.length === 0 ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No job roles configured.
            </div>
          ) : (
            roles.map((item) => (
              <Row
                key={item.id}
                label={item.name}
                description={`${item.department} · ${item.requirements.length} requirement${item.requirements.length === 1 ? '' : 's'}`}
                chevron
                onClick={() => {
                  setSelectedId(item.id)
                  setCreating(false)
                  setEditing(false)
                }}
              />
            ))
          )}
        </InsetGroupedList>
      ) : null}

      <Sheet
        open={selectedId != null}
        onOpenChange={(open) => {
          if (!open) {
            setSelectedId(null)
            setAddingRequirement(false)
            setEditing(false)
          }
        }}
        title={role?.name ?? 'Role'}
      >
        {role ? (
          <div className="space-y-6">
            <div className="rounded-2xl bg-elevated px-4 ring-1 ring-separator/40">
              <DetailRow label="Department" value={role.department} />
              <DetailRow label="Requirements" value={role.requirements.length} />
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={openEdit}
                className="inline-flex min-h-9 items-center gap-1 rounded-lg bg-elevated px-3 text-sm font-medium text-label ring-1 ring-separator/50"
              >
                <Pencil className="h-4 w-4" />
                Edit
              </button>
              <button
                type="button"
                onClick={() => void handleDeleteRole()}
                disabled={deleteRole.isPending}
                className="inline-flex min-h-9 items-center gap-1 rounded-lg bg-elevated px-3 text-sm font-medium text-[var(--color-status-danger)] ring-1 ring-separator/50 disabled:opacity-40"
              >
                <Trash2 className="h-4 w-4" />
                Delete
              </button>
            </div>

            <InsetGroupedList title="Template requirements">
              {role.requirements.length === 0 ? (
                <div className="px-4 py-6 text-center text-sm text-secondary-label">
                  No requirements on this template yet.
                </div>
              ) : (
                role.requirements.map((req) => (
                  <RequirementRow key={req.id} requirement={req} />
                ))
              )}
            </InsetGroupedList>

            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setAddingRequirement(true)}
                className="inline-flex min-h-10 items-center gap-1 rounded-xl bg-elevated px-4 text-sm font-semibold text-label ring-1 ring-separator/50"
              >
                <Plus className="h-4 w-4" />
                Add requirement
              </button>
              <button
                type="button"
                disabled={apply.isPending || role.requirements.length === 0}
                onClick={() => void handleApply()}
                className="inline-flex min-h-10 items-center rounded-xl bg-accent px-4 text-sm font-semibold text-white disabled:opacity-40"
              >
                {apply.isPending ? 'Applying…' : 'Apply to role'}
              </button>
            </div>
          </div>
        ) : null}
      </Sheet>

      <Sheet open={creating} onOpenChange={setCreating} title="Add job role">
        <div className="space-y-4">
          <Field label="Role name">
            <input
              value={roleName}
              onChange={(event) => setRoleName(event.target.value)}
              className={fieldClassName}
              placeholder="e.g. Senior technician"
            />
          </Field>
          <Field label="Department">
            <select
              value={departmentId}
              onChange={(event) =>
                setDepartmentId(event.target.value ? Number(event.target.value) : '')
              }
              className={fieldClassName}
            >
              <option value="">Select department</option>
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>
                  {dept.name}
                </option>
              ))}
            </select>
          </Field>
          <button
            type="button"
            disabled={!roleName.trim() || departmentId === '' || createRole.isPending}
            onClick={() => void handleCreateRole()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            {createRole.isPending ? 'Creating…' : 'Create role'}
          </button>
        </div>
      </Sheet>

      <Sheet open={editing} onOpenChange={setEditing} title="Edit job role">
        <div className="space-y-4">
          <Field label="Role name">
            <input
              value={roleName}
              onChange={(event) => setRoleName(event.target.value)}
              className={fieldClassName}
            />
          </Field>
          <Field label="Department">
            <select
              value={departmentId}
              onChange={(event) =>
                setDepartmentId(event.target.value ? Number(event.target.value) : '')
              }
              className={fieldClassName}
            >
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>
                  {dept.name}
                </option>
              ))}
            </select>
          </Field>
          <button
            type="button"
            disabled={!roleName.trim() || departmentId === '' || updateRole.isPending}
            onClick={() => void handleUpdateRole()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            {updateRole.isPending ? 'Saving…' : 'Save changes'}
          </button>
        </div>
      </Sheet>

      <Sheet
        open={addingRequirement}
        onOpenChange={setAddingRequirement}
        title="Add requirement"
      >
        <div className="space-y-4">
          <Field label="Kind">
            <select
              value={kind}
              onChange={(event) => {
                setKind(event.target.value as RequirementKind)
                setCatalogueId('')
              }}
              className={fieldClassName}
            >
              {KINDS.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </Field>

          <Field label={kind}>
            <select
              value={catalogueId}
              onChange={(event) =>
                setCatalogueId(event.target.value ? Number(event.target.value) : '')
              }
              className={fieldClassName}
            >
              <option value="">Select {kind.toLowerCase()}</option>
              {kind === 'Certification'
                ? certifications.data?.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))
                : null}
              {kind === 'Training'
                ? trainingPrograms.data?.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))
                : null}
            </select>
          </Field>

          <Field label="Due within days of hire (optional)">
            <input
              type="number"
              min={1}
              value={dueWithinDays}
              onChange={(event) => setDueWithinDays(event.target.value)}
              className={fieldClassName}
            />
          </Field>

          <label className="flex items-center gap-2 text-sm text-label">
            <input
              type="checkbox"
              checked={isMandatory}
              onChange={(event) => setIsMandatory(event.target.checked)}
            />
            Mandatory
          </label>

          <button
            type="button"
            disabled={catalogueId === '' || addRequirement.isPending}
            onClick={() => void handleAddRequirement()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Add to template
          </button>
        </div>
      </Sheet>
    </div>
  )
}

function RequirementRow({ requirement }: { requirement: RoleRequirement }) {
  return (
    <div className="border-b border-separator/60 px-4 py-3 last:border-b-0">
      <div className="flex items-start justify-between gap-3">
        <div>
          <div className="font-medium text-label">{requirement.name}</div>
          <div className="text-sm text-secondary-label">
            {requirement.kind}
            {requirement.minimumProficiency
              ? ` · Min ${requirement.minimumProficiency}`
              : null}
            {requirement.dueWithinDaysOfHire
              ? ` · Due within ${requirement.dueWithinDaysOfHire} days of hire`
              : null}
          </div>
        </div>
        <span className="text-xs font-medium text-secondary-label">
          {requirement.isMandatory ? 'Mandatory' : 'Optional'}
        </span>
      </div>
    </div>
  )
}

function ListSkeleton() {
  return (
    <InsetGroupedList>
      {Array.from({ length: 4 }).map((_, index) => (
        <div
          key={index}
          className="animate-pulse border-b border-separator/60 px-4 py-3 last:border-b-0"
        >
          <div className="h-4 w-2/5 rounded bg-separator/50" />
          <div className="mt-2 h-3 w-1/3 rounded bg-separator/40" />
        </div>
      ))}
    </InsetGroupedList>
  )
}

function ErrorCard({ message }: { message: string }) {
  return (
    <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
      <p className="font-medium text-label">{message}</p>
    </div>
  )
}
