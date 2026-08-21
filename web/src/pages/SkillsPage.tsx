import { Plus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import {
  useCreateSkill,
  useDeactivateSkill,
  useSkill,
  useSkillsCatalogue,
  useUpdateSkill,
  type CreateSkillBody,
  type Skill,
  type SkillCategory,
  type UpdateSkillBody,
} from '../api/catalogues'
import { getApiErrorMessage } from '../api/client'
import { DetailRow, Field, fieldClassName, textareaClassName } from '../components/catalogue/formFields'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { Sheet } from '../components/ios/Sheet'
import { cn } from '../lib/cn'

const CATEGORIES: SkillCategory[] = ['Oem', 'EquipmentType', 'SystemType', 'Safety']

const defaultCreateForm = (): CreateSkillBody => ({
  name: '',
  category: 'Oem',
  description: '',
})

export function SkillsPage() {
  const { data: items = [], isLoading, isError } = useSkillsCatalogue()
  const create = useCreateSkill()

  const [search, setSearch] = useState('')
  const [detailId, setDetailId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)
  const [form, setForm] = useState(defaultCreateForm)

  const { data: detail } = useSkill(detailId)

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return items
    return items.filter(
      (item) =>
        item.name.toLowerCase().includes(term)
        || item.category.toLowerCase().includes(term),
    )
  }, [items, search])

  async function submitCreate() {
    try {
      await create.mutateAsync({
        ...form,
        description: form.description || undefined,
      })
      toast.success('Skill created')
      setCreating(false)
    } catch {
      // handled globally
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Skills"
        subtitle="Capability catalogue"
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
          placeholder="Search skills"
          className={cn(fieldClassName, 'pl-10')}
        />
      </div>

      {isLoading ? <ListSkeleton /> : null}
      {isError ? <ErrorCard message="Unable to load skills" /> : null}

      {!isLoading && !isError ? (
        <InsetGroupedList>
          {filtered.length === 0 ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No skills found.
            </div>
          ) : (
            filtered.map((item) => (
              <Row
                key={item.id}
                label={item.name}
                description={item.category}
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
        title={detail?.name ?? 'Skill'}
      >
        {detail ? (
          <SkillDetail item={detail} onClose={() => setDetailId(null)} />
        ) : null}
      </Sheet>

      <Sheet open={creating} onOpenChange={setCreating} title="New skill">
        <div className="space-y-4">
          <Field label="Name">
            <input
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              className={fieldClassName}
            />
          </Field>
          <Field label="Category">
            <select
              value={form.category}
              onChange={(event) =>
                setForm({ ...form, category: event.target.value as SkillCategory })
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
          <Field label="Description">
            <textarea
              value={form.description ?? ''}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              rows={3}
              className={textareaClassName}
            />
          </Field>
          <button
            type="button"
            disabled={!form.name || create.isPending}
            onClick={() => void submitCreate()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Create skill
          </button>
        </div>
      </Sheet>
    </div>
  )
}

function SkillDetail({ item, onClose }: { item: Skill; onClose: () => void }) {
  const update = useUpdateSkill()
  const deactivate = useDeactivateSkill()
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState<UpdateSkillBody>({
    name: item.name,
    category: item.category,
    description: item.description ?? '',
  })

  async function handleSave() {
    try {
      await update.mutateAsync({
        id: item.id,
        ...form,
        description: form.description || undefined,
      })
      toast.success('Skill updated')
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
      toast.success('Skill deactivated')
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
        <Field label="Category">
          <select
            value={form.category ?? 'Oem'}
            onChange={(event) =>
              setForm({ ...form, category: event.target.value as SkillCategory })
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
        <Field label="Description">
          <textarea
            value={form.description ?? ''}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
            rows={3}
            className={textareaClassName}
          />
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
            disabled={!form.name || update.isPending}
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
    <div className="space-y-4">
      <div className="rounded-2xl bg-elevated px-4 ring-1 ring-separator/40">
        <DetailRow label="Category" value={item.category} />
        <DetailRow label="Active" value={item.isActive ? 'Yes' : 'No'} />
      </div>
      {item.description ? (
        <p className="text-sm text-secondary-label">{item.description}</p>
      ) : (
        <p className="text-sm text-secondary-label">No description provided.</p>
      )}
      <p className="text-xs text-secondary-label">
        Skills are granted when employees complete linked certifications or training programs.
      </p>
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="min-h-11 flex-1 rounded-xl bg-accent text-sm font-semibold text-white"
        >
          Edit
        </button>
        <button
          type="button"
          onClick={() => void handleDeactivate()}
          disabled={deactivate.isPending}
          className="min-h-11 flex-1 rounded-xl bg-status-danger-bg text-sm font-semibold text-status-danger"
        >
          Deactivate
        </button>
      </div>
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
