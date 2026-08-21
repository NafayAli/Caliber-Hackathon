import { Plus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import {
  useCertification,
  useCertifications,
  useCreateCertification,
  useDeactivateCertification,
  useSetCertificationGrantedSkills,
  useSkillsCatalogue,
  useUpdateCertification,
  type Certification,
  type CertificationCategory,
  type CreateCertificationBody,
  type SkillGrantInput,
  type UpdateCertificationBody,
} from '../api/catalogues'
import { getApiErrorMessage } from '../api/client'
import { BoolLabel, DetailRow, Field, fieldClassName, textareaClassName } from '../components/catalogue/formFields'
import { GrantedSkillsEditor } from '../components/catalogue/GrantedSkillsEditor'
import { GrantedSkillsList } from '../components/catalogue/GrantedSkillsList'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { Sheet } from '../components/ios/Sheet'
import { cn } from '../lib/cn'

const CATEGORIES: CertificationCategory[] = ['Oem', 'Safety', 'Regulatory', 'Internal']

const defaultCreateForm = (): CreateCertificationBody => ({
  name: '',
  code: '',
  category: 'Oem',
  issuingBody: '',
  description: '',
  validityMonths: undefined,
  expiryWarningDays: 60,
  requiresEvidence: false,
  grantedSkills: [],
})

export function CertificationsPage() {
  const { data: items = [], isLoading, isError } = useCertifications()
  const create = useCreateCertification()

  const [search, setSearch] = useState('')
  const [detailId, setDetailId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)
  const [form, setForm] = useState(defaultCreateForm)

  const { data: detail } = useCertification(detailId)
  const { data: skills = [] } = useSkillsCatalogue()
  const [createGrants, setCreateGrants] = useState<SkillGrantInput[]>([])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return items
    return items.filter(
      (item) =>
        item.name.toLowerCase().includes(term)
        || item.code.toLowerCase().includes(term)
        || item.issuingBody.toLowerCase().includes(term),
    )
  }, [items, search])

  function openCreate() {
    setForm(defaultCreateForm())
    setCreateGrants([])
    setCreating(true)
    setDetailId(null)
  }

  async function submitCreate() {
    try {
      await create.mutateAsync({
        ...form,
        description: form.description || undefined,
        validityMonths: form.validityMonths || undefined,
        grantedSkills: createGrants,
      })
      toast.success('Certification created')
      setCreating(false)
    } catch {
      // toast from mutation handler
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Certifications"
        subtitle="Credential catalogue"
        actions={
          <button
            type="button"
            onClick={openCreate}
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
          placeholder="Search certifications"
          className={cn(fieldClassName, 'pl-10')}
        />
      </div>

      {isLoading ? <ListSkeleton /> : null}
      {isError ? <ErrorCard message="Unable to load certifications" /> : null}

      {!isLoading && !isError ? (
        <InsetGroupedList>
          {filtered.length === 0 ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No certifications found.
            </div>
          ) : (
            filtered.map((item) => (
              <Row
                key={item.id}
                label={item.name}
                description={`${item.code} · ${item.category} · ${item.issuingBody}`}
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
        title={detail?.name ?? 'Certification'}
      >
        {detail ? (
          <CertificationDetail
            item={detail}
            onClose={() => setDetailId(null)}
            skills={skills}
          />
        ) : null}
      </Sheet>

      <Sheet
        open={creating}
        onOpenChange={setCreating}
        title="New certification"
      >
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
                setForm({ ...form, category: event.target.value as CertificationCategory })
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
          <Field label="Issuing body">
            <input
              value={form.issuingBody}
              onChange={(event) => setForm({ ...form, issuingBody: event.target.value })}
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
          <Field label="Validity (months)">
            <input
              type="number"
              min={1}
              value={form.validityMonths ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  validityMonths: event.target.value ? Number(event.target.value) : undefined,
                })
              }
              className={fieldClassName}
            />
          </Field>
          <Field label="Expiry warning (days)">
            <input
              type="number"
              min={1}
              value={form.expiryWarningDays ?? 60}
              onChange={(event) =>
                setForm({ ...form, expiryWarningDays: Number(event.target.value) })
              }
              className={fieldClassName}
            />
          </Field>
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
            disabled={!form.name || !form.code || !form.issuingBody || create.isPending}
            onClick={() => void submitCreate()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Create certification
          </button>
        </div>
      </Sheet>
    </div>
  )
}

function CertificationDetail({
  item,
  onClose,
  skills,
}: {
  item: Certification
  onClose: () => void
  skills: import('../api/catalogues').Skill[]
}) {
  const update = useUpdateCertification()
  const deactivate = useDeactivateCertification()
  const setGrantedSkills = useSetCertificationGrantedSkills(item.id)
  const [editing, setEditing] = useState(false)
  const [editingGrants, setEditingGrants] = useState(false)
  const [grants, setGrants] = useState<SkillGrantInput[]>(
    item.grantedSkills.map((skill) => ({
      skillId: skill.skillId,
      grantedProficiency: skill.grantedProficiency,
    })),
  )
  const [form, setForm] = useState<UpdateCertificationBody>({
    name: item.name,
    code: item.code,
    category: item.category,
    issuingBody: item.issuingBody,
    description: item.description ?? '',
    validityMonths: item.validityMonths ?? undefined,
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
        validityMonths: form.validityMonths || undefined,
      })
      toast.success('Certification updated')
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
      toast.success('Certification deactivated')
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
        <Field label="Category">
          <select
            value={form.category ?? 'Oem'}
            onChange={(event) =>
              setForm({ ...form, category: event.target.value as CertificationCategory })
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
        <Field label="Issuing body">
          <input
            value={form.issuingBody ?? ''}
            onChange={(event) => setForm({ ...form, issuingBody: event.target.value })}
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
        <DetailRow label="Issuing body" value={item.issuingBody} />
        <DetailRow
          label="Validity"
          value={item.validityMonths ? `${item.validityMonths} months` : 'No expiry'}
        />
        <DetailRow label="Warning window" value={`${item.expiryWarningDays} days`} />
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
