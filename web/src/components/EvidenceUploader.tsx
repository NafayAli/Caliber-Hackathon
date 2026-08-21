import { useRef, useState, type ReactNode } from 'react'
import { Upload } from 'lucide-react'
import { toast } from 'sonner'
import { getPersonaId } from '../api/persona'
import type { EvidenceItem, EvidenceType } from '../api/employees'
import type { ProblemDetails } from '../api/types'
import { Sheet } from './ios/Sheet'
import { cn } from '../lib/cn'

const ACCEPT = '.pdf,.png,.jpg,.jpeg,.webp'

interface EvidenceUploadOptions {
  employeeCertificationId?: number
  employeeTrainingId?: number
  employeeSkillId?: number
  evidenceType?: EvidenceType
}

interface EvidenceUploaderProps {
  employeeId: number
  employeeCertificationId?: number
  employeeTrainingId?: number
  employeeSkillId?: number
  evidenceType?: EvidenceType
  onUploaded: (item: EvidenceItem) => void
  className?: string
  compact?: boolean
}

function parseUploadError(problem: ProblemDetails | null): string {
  if (!problem) return 'Upload failed'

  if (problem.errors) {
    const messages = Object.values(problem.errors).flat()
    if (messages.length > 0) return messages.join(' ')
  }

  return problem.detail ?? problem.title ?? 'Upload failed'
}

export async function uploadEvidenceFile(
  employeeId: number,
  file: File,
  options: EvidenceUploadOptions = {},
): Promise<EvidenceItem> {
  const hasLink =
    options.employeeCertificationId != null
    || options.employeeTrainingId != null
    || options.employeeSkillId != null

  const resolvedType: EvidenceType = options.evidenceType ?? (hasLink ? 'Certificate' : 'General')

  const form = new FormData()
  form.append('EmployeeId', String(employeeId))
  form.append('EvidenceType', resolvedType)
  if (options.employeeCertificationId != null) {
    form.append('EmployeeCertificationId', String(options.employeeCertificationId))
  }
  if (options.employeeTrainingId != null) {
    form.append('EmployeeTrainingId', String(options.employeeTrainingId))
  }
  if (options.employeeSkillId != null) {
    form.append('EmployeeSkillId', String(options.employeeSkillId))
  }
  form.append('file', file)

  const headers = new Headers()
  const personaId = getPersonaId()
  if (personaId) {
    headers.set('X-Persona-Id', personaId)
  }

  const response = await fetch('/api/evidence', {
    method: 'POST',
    credentials: 'include',
    headers,
    body: form,
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    throw new Error(parseUploadError(problem))
  }

  return (await response.json()) as EvidenceItem
}

export function EvidenceUploader({
  employeeId,
  employeeCertificationId,
  employeeTrainingId,
  employeeSkillId,
  evidenceType,
  onUploaded,
  className,
  compact = false,
}: EvidenceUploaderProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)

  async function handleFile(file: File) {
    setUploading(true)
    try {
      const item = await uploadEvidenceFile(employeeId, file, {
        employeeCertificationId,
        employeeTrainingId,
        employeeSkillId,
        evidenceType,
      })
      onUploaded(item)
      toast.success('Evidence uploaded')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Upload failed')
    } finally {
      setUploading(false)
      if (inputRef.current) {
        inputRef.current.value = ''
      }
    }
  }

  return (
    <div className={className}>
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPT}
        className="sr-only"
        onChange={(event) => {
          const file = event.target.files?.[0]
          if (file) void handleFile(file)
        }}
      />
      <button
        type="button"
        disabled={uploading}
        onClick={() => inputRef.current?.click()}
        className={cn(
          'inline-flex min-h-11 items-center gap-2 rounded-xl bg-accent text-sm font-semibold text-white',
          compact ? 'px-3' : 'px-4',
          'transition-opacity hover:opacity-90 active:opacity-75 disabled:opacity-50',
        )}
      >
        <Upload className="h-4 w-4" />
        {uploading ? 'Uploading…' : compact ? 'Upload' : 'Upload evidence'}
      </button>
    </div>
  )
}

export function AwardEvidenceField({
  file,
  onFileChange,
}: {
  file: File | null
  onFileChange: (file: File | null) => void
}) {
  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div className="rounded-xl bg-elevated px-4 py-3 ring-1 ring-separator/50">
      <p className="text-sm font-medium text-label">Certificate evidence (optional)</p>
      <p className="mt-1 text-xs text-secondary-label">
        Attach a PDF or image of the certificate. Managers can verify it after upload.
      </p>
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPT}
        className="sr-only"
        onChange={(event) => onFileChange(event.target.files?.[0] ?? null)}
      />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        className="mt-3 inline-flex min-h-9 items-center gap-2 rounded-lg bg-accent-muted px-3 text-xs font-semibold text-accent"
      >
        <Upload className="h-3.5 w-3.5" />
        {file ? file.name : 'Choose file'}
      </button>
      {file ? (
        <button
          type="button"
          onClick={() => {
            onFileChange(null)
            if (inputRef.current) inputRef.current.value = ''
          }}
          className="ml-2 text-xs font-medium text-secondary-label hover:text-label"
        >
          Clear
        </button>
      ) : null}
    </div>
  )
}

export function EvidencePreview({ item }: { item: EvidenceItem }) {
  const src = `/api/evidence/${item.id}/content`
  const isImage = item.contentType.startsWith('image/')
  const isPdf = item.contentType === 'application/pdf'

  if (isImage) {
    return (
      <img
        src={src}
        alt={item.fileName}
        className="max-h-64 w-full rounded-xl object-contain bg-elevated"
      />
    )
  }

  if (isPdf) {
    return (
      <iframe
        title={item.fileName}
        src={src}
        className="h-64 w-full rounded-xl border border-separator/60 bg-elevated"
      />
    )
  }

  return (
    <a
      href={`/api/evidence/${item.id}/download`}
      target="_blank"
      rel="noreferrer"
      className="text-sm font-medium text-accent hover:underline"
    >
      Download {item.fileName}
    </a>
  )
}

export function EvidencePreviewModal({
  open,
  onOpenChange,
  item,
  footer,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  item: EvidenceItem | null
  footer?: ReactNode
}) {
  if (!item) return null

  return (
    <Sheet open={open} onOpenChange={onOpenChange} title={item.fileName} className="max-w-2xl">
      <div className="space-y-3">
        <div className="text-sm text-secondary-label">
          {item.evidenceType} · {formatFileSize(item.sizeBytes)} ·{' '}
          {new Date(item.uploadedOn).toLocaleDateString()}
        </div>
        {item.isVerified ? (
          <p className="text-xs text-[var(--color-status-compliant)]">
            Verified{item.verifiedBy ? ` by ${item.verifiedBy}` : ''}
          </p>
        ) : (
          <p className="text-xs text-secondary-label">
            Pending verification — a manager or admin can verify this from the employee profile.
          </p>
        )}
        <EvidencePreview item={item} />
        {footer}
      </div>
    </Sheet>
  )
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function evidenceForCertification(
  evidence: EvidenceItem[],
  employeeCertificationId: number,
): EvidenceItem | undefined {
  return evidence.find((item) => item.employeeCertificationId === employeeCertificationId)
}
