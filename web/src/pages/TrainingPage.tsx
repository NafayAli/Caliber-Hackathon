import { Plus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import {
  useCreateTrainingProgram,
  useDeactivateTrainingProgram,
  useSetTrainingGrantedSkills,
  useSkillsCatalogue,
  useTrainingProgram,
  useTrainingPrograms,
  useUpdateTrainingProgram,
  type CreateTrainingProgramBody,
  type DeliveryMode,
  type SkillGrantInput,
  type TrainingCategory,
  type TrainingProgram,
  type UpdateTrainingProgramBody,
} from '../api/catalogues'
import { getApiErrorMessage } from '../api/client'
import { BoolLabel, DetailRow, Field, fieldClassName, textareaClassName } from '../components/catalogue/formFields'
import { GrantedSkillsEditor } from '../components/catalogue/GrantedSkillsEditor'
import { GrantedSkillsList } from '../components/catalogue/GrantedSkillsList'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { Sheet } from '../components/ios/Sheet'
import { cn } from '../lib/cn'

const CATEGORIES: TrainingCategory[] = [
  'Oem',
  'Safety',
  'Onboarding',
  'Product',
  'Internal',
]

const DELIVERY_MODES: DeliveryMode[] = ['Online', 'InPerson', 'OnTheJob', 'Document']

const defaultCreateForm = (): CreateTrainingProgramBody => ({
  name: '',
  code: '',
  category: 'Safety',
  provider: '',
  description: '',
  deliveryMode: 'Online',
  estimatedDurationHours: 1,
  requiresAcknowledgement: false,
  recurrenceMonths: undefined,
  expiryWarningDays: 60,
  requiresEvidence: false,
})

export function TrainingPage() {
  const { data: items = [], isLoading, isError } = useTrainingPrograms()
  const create = useCreateTrainingProgram()

  const [search, setSearch] = useState('')
  const [detailId, setDetailId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)
  const [form, setForm] = useState(defaultCreateForm)

  const { data: detail } = useTrainingProgram(detailId)
  const { data: skills = [] } = useSkillsCatalogue()
  const [createGrants, setCreateGrants] = useState<SkillGrantInput[]>([])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return items
    return items.filter(
      (item) =>
        item.name.toLowerCase().includes(term)
        || item.code.toLowerCase().includes(term)
        || item.provider.toLowerCase().includes(term),
    )
  }, [items, search])

  async function submitCreate() {
    try {
      await create.mutateAsync({
        ...form,
        description: form.description || undefined,
        recurrenceMonths: form.recurrenceMonths || undefined,
        grantedSkills: createGrants,
      })
      toast.success('Training program created')
      setCreating(false)
    } catch {
      // handled globally
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Training"
        subtitle="Programs and assignments"
        actions={
          <button
            type="button"
            onClick={() => {
              setForm(defaultCreateForm())
              setCreating(true)
              setDetailId(null)
            }}
            className="inline-flex min-h-10 items-center gap-1 rounded-xl bg-accent px-4 text-sm font-semibold text-white"
          >
            <Plus className="h-4 w-4" />
            Add
          </button>
        }
      />

      <div className="relative mb-4">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-label" />
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search training programs"
          className={cn(fieldClassName, 'pl-10')}
        />
      </div>

      {isLoading ? <ListSkeleton /> : null}
      {isError ? <ErrorCard message="Unable to load training programs" /> : null}

      {!isLoading && !isError ? (
        <InsetGroupedList>
          {filtered.length === 0 ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No training programs found.
            </div>
          ) : (
            filtered.map((item) => (
              <Row
                key={item.id}
                label={item.name}
                description={`${item.code} · ${item.category} · ${item.deliveryMode}`}
                accessory={
                  item.grantedSkills.length > 0 ? (
                    <span className="text-xs text-secondary-label">
                      {item.grantedSkills.length} skill{item.grantedSkills.length === 1 ? '' : 's'}
                    </span>
                  ) : null
                }
                chevron
                onClick={() => {
                  setDetailId(item.id)
                  setCreating(false)
                }}
              />
            ))
          )}
        </InsetGroupedList>
      ) : null}

      <Sheet
        open={detailId != null}
        onOpenChange={(open) => {
          if (!open) setDetailId(null)
        }}
        title={detail?.name ?? 'Training program'}
      >
        {detail ? (
          <TrainingDetail item={detail} onClose={() => setDetailId(null)} skills={skills} />
        ) : null}
      </Sheet>

      <Sheet open={creating} onOpenChange={setCreating} title="New training program">
        <div className="space-y-4">
          <Field label="Name">
            <input
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              className={fieldClassName}
            />
          </Field>
          <Field label="Code">
            <input
              value={form.code}
              onChange={(event) => setForm({ ...form, code: event.target.value })}
              className={fieldClassName}
            />
          </Field>
          <Field label="Category">
            <select
              value={form.category}
              onChange={(event) =>
                setForm({ ...form, category: event.target.value as TrainingCategory })
              }
              className={fieldClassName}
            >
              {CATEGORIES.map((category) => (
                <option key={category} value={category}>
                  {category}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Provider">
            <input
              value={form.provider}
              onChange={(event) => setForm({ ...form, provider: event.target.value })}
              className={fieldClassName}
            />
          </Field>
          <Field label="Delivery mode">
            <select
              value={form.deliveryMode}
              onChange={(event) =>
                setForm({ ...form, deliveryMode: event.target.value as DeliveryMode })
              }
              className={fieldClassName}
            >
              {DELIVERY_MODES.map((mode) => (
                <option key={mode} value={mode}>
                  {mode}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Estimated hours">
            <input
              type="number"
              min={0.5}
              step={0.5}
              value={form.estimatedDurationHours}
              onChange={(event) =>
                setForm({ ...form, estimatedDurationHours: Number(event.target.value) })
              }
              className={fieldClassName}
            />
          </Field>
          <Field label="Recurrence (months)">
            <input
              type="number"
              min={1}
              value={form.recurrenceMonths ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  recurrenceMonths: event.target.value ? Number(event.target.value) : undefined,
                })
              }
              className={fieldClassName}
            />
          </Field>
          <Field label="Description">
            <textarea
              value={form.description ?? ''}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              rows={3}
              className={textareaClassName}
            />
          </Field>
          <label className="flex items-center gap-2 text-sm text-label">
            <input
              type="checkbox"
              checked={form.requiresAcknowledgement}
              onChange={(event) =>
                setForm({ ...form, requiresAcknowledgement: event.target.checked })
              }
            />
            Requires acknowledgement
          </label>
          <label className="flex items-center gap-2 text-sm text-label">
            <input
              type="checkbox"
              checked={form.requiresEvidence}
              onChange={(event) =>
                setForm({ ...form, requiresEvidence: event.target.checked })
              }
            />
            Requires evidence
          </label>
          <GrantedSkillsEditor grants={createGrants} skills={skills} onChange={setCreateGrants} />
          <button
            type="button"
            disabled={!form.name || !form.code || !form.provider || create.isPending}
            onClick={() => void submitCreate()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Create program
          </button>
        </div>
      </Sheet>
    </div>
  )
}

function TrainingDetail({
  item,
  onClose,
  skills,
}: {
  item: TrainingProgram
  onClose: () => void
  skills: import('../api/catalogues').Skill[]
}) {
  const update = useUpdateTrainingProgram()
  const deactivate = useDeactivateTrainingProgram()
  const setGrantedSkills = useSetTrainingGrantedSkills(item.id)
  const [editing, setEditing] = useState(false)
  const [editingGrants, setEditingGrants] = useState(false)
  const [grants, setGrants] = useState<SkillGrantInput[]>(
    item.grantedSkills.map((skill) => ({
      skillId: skill.skillId,
      grantedProficiency: skill.grantedProficiency,
    })),
  )
  const [form, setForm] = useState<UpdateTrainingProgramBody>({
    name: item.name,
    code: item.code,
    category: item.category,
    provider: item.provider,
    description: item.description ?? '',
    deliveryMode: item.deliveryMode,
    estimatedDurationHours: item.estimatedDurationHours,
    requiresAcknowledgement: item.requiresAcknowledgement,
    recurrenceMonths: item.recurrenceMonths ?? undefined,
    expiryWarningDays: item.expiryWarningDays,
    requiresEvidence: item.requiresEvidence,
  })

  async function handleSaveGrants() {
    try {
      await setGrantedSkills.mutateAsync(grants)
      toast.success('Granted skills updated')
      setEditingGrants(false)
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleSave() {
    try {
      await update.mutateAsync({
        id: item.id,
        ...form,
        description: form.description || undefined,
        recurrenceMonths: form.recurrenceMonths || undefined,
      })
      toast.success('Training program updated')
      setEditing(false)
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleDeactivate() {
    if (!window.confirm(`Deactivate "${item.name}"? It will no longer appear in assign lists.`)) {
      return
    }
    try {
      await deactivate.mutateAsync(item.id)
      toast.success('Training program deactivated')
      onClose()
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  if (editing) {
    return (
      <div className="space-y-4">
        <Field label="Name">
          <input
            value={form.name ?? ''}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
            className={fieldClassName}
          />
        </Field>
        <Field label="Code">
          <input
            value={form.code ?? ''}
            onChange={(event) => setForm({ ...form, code: event.target.value })}
            className={fieldClassName}
          />
        </Field>
        <Field label="Provider">
          <input
            value={form.provider ?? ''}
            onChange={(event) => setForm({ ...form, provider: event.target.value })}
            className={fieldClassName}
          />
        </Field>
        <Field label="Delivery mode">
          <select
            value={form.deliveryMode ?? 'Online'}
            onChange={(event) =>
              setForm({ ...form, deliveryMode: event.target.value as DeliveryMode })
            }
            className={fieldClassName}
          >
            {DELIVERY_MODES.map((mode) => (
              <option key={mode} value={mode}>
                {mode}
              </option>
            ))}
          </select>
        </Field>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setEditing(false)}
            className="min-h-11 flex-1 rounded-xl bg-elevated text-sm font-semibold text-label ring-1 ring-separator/50"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={update.isPending}
            onClick={() => void handleSave()}
            className="min-h-11 flex-1 rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Save
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="min-h-10 flex-1 rounded-xl bg-accent text-sm font-semibold text-white"
        >
          Edit
        </button>
        {item.isActive ? (
          <button
            type="button"
            disabled={deactivate.isPending}
            onClick={() => void handleDeactivate()}
            className="min-h-10 flex-1 rounded-xl bg-status-danger-bg text-sm font-semibold text-status-danger disabled:opacity-40"
          >
            Deactivate
          </button>
        ) : null}
      </div>
      <div className="rounded-2xl bg-elevated px-4 ring-1 ring-separator/40">
        <DetailRow label="Code" value={item.code} />
        <DetailRow label="Category" value={item.category} />
        <DetailRow label="Provider" value={item.provider} />
        <DetailRow label="Delivery" value={item.deliveryMode} />
        <DetailRow label="Duration" value={`${item.estimatedDurationHours}h`} />
        <DetailRow
          label="Recurrence"
          value={item.recurrenceMonths ? `${item.recurrenceMonths} months` : 'One-time'}
        />
        <DetailRow label="Acknowledgement" value={<BoolLabel value={item.requiresAcknowledgement} />} />
        <DetailRow label="Requires evidence" value={<BoolLabel value={item.requiresEvidence} />} />
      </div>
      {item.description ? (
        <p className="text-sm text-secondary-label">{item.description}</p>
      ) : null}
      {editingGrants ? (
        <div className="space-y-4">
          <GrantedSkillsEditor grants={grants} skills={skills} onChange={setGrants} />
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setEditingGrants(false)}
              className="min-h-10 flex-1 rounded-xl bg-elevated text-sm font-semibold text-label ring-1 ring-separator/50"
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={setGrantedSkills.isPending}
              onClick={() => void handleSaveGrants()}
              className="min-h-10 flex-1 rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
            >
              Save skills
            </button>
          </div>
        </div>
      ) : (
        <>
          <GrantedSkillsList skills={item.grantedSkills} />
          <button
            type="button"
            onClick={() => {
              setGrants(
                item.grantedSkills.map((skill) => ({
                  skillId: skill.skillId,
                  grantedProficiency: skill.grantedProficiency,
                })),
              )
              setEditingGrants(true)
            }}
            className="min-h-10 w-full rounded-xl bg-elevated text-sm font-semibold text-label ring-1 ring-separator/50"
          >
            Edit granted skills
          </button>
        </>
      )}
    </div>
  )
}

function ListSkeleton() {
  return (
    <InsetGroupedList>
      {Array.from({ length: 5 }).map((_, index) => (
        <div
          key={index}
          className="animate-pulse border-b border-separator/60 px-4 py-3 last:border-b-0"
        >
          <div className="h-4 w-2/5 rounded bg-separator/50" />
          <div className="mt-2 h-3 w-3/5 rounded bg-separator/40" />
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
