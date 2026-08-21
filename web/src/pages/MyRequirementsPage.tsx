import { Eye } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  employeeKeys,
  useAcknowledgeTraining,
  useCompleteTraining,
  useDeleteEvidence,
  useEmployee,
  useRecordAward,
  useUpdateTrainingProgress,
  useWaiveRequirement,
  type EvidenceItem,
  type RequirementStatus,
} from '../api/employees'
import { useRequestRenewal } from '../api/renewals'
import { computeReadinessPercent, useMyRequirements } from '../api/me'
import { isManagerOrAdmin, useAuth } from '../contexts/AuthContext'
import {
  AwardEvidenceField,
  EvidencePreviewModal,
  uploadEvidenceFile,
} from '../components/EvidenceUploader'
import { Avatar } from '../components/ios/Avatar'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { SegmentedControl } from '../components/ios/SegmentedControl'
import { Sheet } from '../components/ios/Sheet'
import { ReadinessBar, StatusChip } from '../components/StatusChip'
import { usePersona } from '../contexts/PersonaContext'

type MyTab = 'requirements' | 'skills'
type ActionKind = 'start' | 'complete' | 'award' | 'waive'

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10)
}

export function MyRequirementsPage() {
  const { activePersona, personaId } = usePersona()
  const queryClient = useQueryClient()
  const { accessLevel } = useAuth()
  const employeeId = personaId ?? 0

  const { data: requirements = [], isLoading, isError } = useMyRequirements()
  const { data: profile } = useEmployee(employeeId)

  const [tab, setTab] = useState<MyTab>('requirements')
  const [sheet, setSheet] = useState<ActionKind | null>(null)
  const [activeRequirement, setActiveRequirement] = useState<RequirementStatus | null>(null)
  const [previewEvidence, setPreviewEvidence] = useState<EvidenceItem | null>(null)
  const [completeDate, setCompleteDate] = useState(todayIso())
  const [awardDate, setAwardDate] = useState(todayIso())
  const [certificateNumber, setCertificateNumber] = useState('')
  const [awardEvidenceFile, setAwardEvidenceFile] = useState<File | null>(null)
  const [waiveReason, setWaiveReason] = useState('')

  const startTraining = useUpdateTrainingProgress(employeeId)
  const completeTraining = useCompleteTraining(employeeId)
  const acknowledgeTraining = useAcknowledgeTraining(employeeId)
  const recordAward = useRecordAward(employeeId)
  const waiveRequirement = useWaiveRequirement(employeeId)
  const deleteEvidence = useDeleteEvidence(employeeId)
  const requestRenewal = useRequestRenewal()

  const readinessPercent = useMemo(
    () => computeReadinessPercent(requirements),
    [requirements],
  )

  const evidenceByCertId = useMemo(() => {
    const map = new Map<number, EvidenceItem>()
    for (const item of profile?.evidence ?? []) {
      if (item.employeeCertificationId != null) {
        map.set(item.employeeCertificationId, item)
      }
    }
    return map
  }, [profile?.evidence])

  const tabOptions = useMemo(
    () => [
      { value: 'requirements' as const, label: 'Requirements' },
      { value: 'skills' as const, label: 'Skills' },
    ],
    [],
  )

  function invalidateData() {
    void queryClient.invalidateQueries({ queryKey: ['me', 'requirements', personaId] })
    void queryClient.invalidateQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
  }

  function openAction(kind: ActionKind, requirement: RequirementStatus) {
    setActiveRequirement(requirement)
    setCompleteDate(todayIso())
    setAwardDate(todayIso())
    setCertificateNumber('')
    setAwardEvidenceFile(null)
    setWaiveReason('')
    setSheet(kind)
  }

  async function submitSheet() {
    if (!activeRequirement) return
    try {
      if (sheet === 'start') {
        await startTraining.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          status: 'InProgress',
          startedOn: todayIso(),
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Training started')
      } else if (sheet === 'complete') {
        await completeTraining.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          completedOn: completeDate,
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Training marked complete')
      } else if (sheet === 'award') {
        await recordAward.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          awardedOn: awardDate,
          certificateNumber: certificateNumber || undefined,
          rowVersion: activeRequirement.rowVersion,
        })
        if (awardEvidenceFile) {
          await uploadEvidenceFile(employeeId, awardEvidenceFile, {
            employeeCertificationId: activeRequirement.sourceId,
            evidenceType: 'Certificate',
          })
          toast.success('Award recorded with evidence')
        } else {
          toast.success('Award recorded')
        }
        invalidateData()
      } else if (sheet === 'waive') {
        await waiveRequirement.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          reason: waiveReason,
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Requirement waived')
      }
      setSheet(null)
      setActiveRequirement(null)
      setAwardEvidenceFile(null)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Action failed')
    }
  }

  async function handleRequestRenewal(requirement: RequirementStatus) {
    try {
      await requestRenewal.mutateAsync({
        kind: requirement.kind,
        assignmentId: requirement.sourceId,
      })
      toast.success('Renewal request submitted')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Request failed')
    }
  }

  async function handleAcknowledge(requirement: RequirementStatus) {
    try {
      await acknowledgeTraining.mutateAsync({
        assignmentId: requirement.sourceId,
        rowVersion: requirement.rowVersion,
      })
      toast.success('Training acknowledged')
    } catch {
      // global handler
    }
  }

  if (!activePersona) {
    return (
      <div className="mx-auto max-w-3xl">
        <LargeTitleHeader title="My requirements" subtitle="Select a persona to continue" />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader title="My requirements" subtitle="Your certifications and training" />

      <header className="mb-6 flex items-start gap-4">
        <Avatar name={activePersona.displayName} size="lg" />
        <div className="min-w-0 flex-1">
          <h2 className="text-[28px] font-bold leading-tight text-label">
            {activePersona.displayName}
          </h2>
          <p className="mt-1 text-secondary-label">
            {activePersona.jobRole} · {activePersona.location}
          </p>
          {!isLoading ? (
            <div className="mt-3 space-y-1">
              <ReadinessBar percent={readinessPercent} />
              <p className="text-sm tabular-nums text-secondary-label">
                {readinessPercent}% readiness
              </p>
            </div>
          ) : null}
        </div>
      </header>

      <SegmentedControl
        value={tab}
        options={tabOptions}
        onChange={(value) => setTab(value as MyTab)}
        className="mb-6"
      />

      {tab === 'requirements' ? (
        <>
          {isLoading ? <RequirementsSkeleton /> : null}
          {isError ? (
            <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
              <p className="font-medium text-label">Unable to load your requirements</p>
            </div>
          ) : null}
          {!isLoading && !isError ? (
            <InsetGroupedList title="Certifications & training">
              {requirements.length === 0 ? (
                <div className="px-4 py-8 text-center text-sm text-secondary-label">
                  No requirements assigned yet.
                </div>
              ) : (
                requirements.map((req) => {
                  const linkedEvidence =
                    req.kind === 'Certification' ? evidenceByCertId.get(req.sourceId) : undefined

                  return (
                    <div
                      key={`${req.kind}-${req.sourceId}`}
                      className="border-b border-separator/60 px-4 py-3 last:border-b-0"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <div className="font-medium text-label">{req.name}</div>
                          <div className="text-sm text-secondary-label">
                            {req.kind} · {req.category}
                            {req.dueOn ? ` · Due ${formatDate(req.dueOn)}` : null}
                            {req.effectiveDate ? ` · Expires ${formatDate(req.effectiveDate)}` : null}
                          </div>
                          {linkedEvidence ? (
                            <p className="mt-1 text-xs text-secondary-label">
                              {linkedEvidence.isVerified
                                ? 'Evidence verified'
                                : 'Evidence pending verification — your manager will review it'}
                            </p>
                          ) : null}
                        </div>
                        <StatusChip status={req.status} />
                      </div>
                      <div className="mt-2 flex flex-wrap items-center gap-2">
                        {linkedEvidence ? (
                          <button
                            type="button"
                            onClick={() => setPreviewEvidence(linkedEvidence)}
                            className="inline-flex items-center gap-1 rounded-lg bg-elevated px-3 py-1.5 text-xs font-semibold text-label ring-1 ring-separator/50"
                          >
                            <Eye className="h-3.5 w-3.5" />
                            Preview evidence
                          </button>
                        ) : null}
                        <MyRequirementActions
                          requirement={req}
                          canWaive={isManagerOrAdmin(accessLevel)}
                          onAction={openAction}
                          onAcknowledge={handleAcknowledge}
                          onRequestRenewal={handleRequestRenewal}
                        />
                      </div>
                    </div>
                  )
                })
              )}
            </InsetGroupedList>
          ) : null}
        </>
      ) : null}

      {tab === 'skills' ? (
        <InsetGroupedList title="My skills">
          {!profile?.skills.length ? (
            <div className="px-4 py-8 text-center text-sm text-secondary-label">
              No skills recorded yet.
            </div>
          ) : (
            profile.skills.map((skill) => (
              <Row
                key={skill.id}
                label={skill.skillName}
                description={`${skill.proficiencyLevel} · ${skill.sourceType} · Assessed ${formatDate(skill.assessedOn)}`}
              />
            ))
          )}
        </InsetGroupedList>
      ) : null}

      <EvidencePreviewModal
        open={previewEvidence != null}
        onOpenChange={(open) => {
          if (!open) setPreviewEvidence(null)
        }}
        item={previewEvidence}
        footer={
          previewEvidence ? (
            <button
              type="button"
              onClick={() => {
                deleteEvidence.mutate(previewEvidence.id, {
                  onSuccess: () => {
                    toast.success('Evidence deleted')
                    setPreviewEvidence(null)
                    invalidateData()
                  },
                })
              }}
              className="rounded-lg bg-elevated px-4 py-2 text-sm font-semibold text-[var(--color-status-danger)] ring-1 ring-separator/50"
            >
              Delete
            </button>
          ) : null
        }
      />

      <Sheet
        open={sheet != null}
        onOpenChange={(open) => {
          if (!open) {
            setSheet(null)
            setActiveRequirement(null)
            setAwardEvidenceFile(null)
          }
        }}
        title={
          sheet === 'start'
            ? 'Start training'
            : sheet === 'complete'
              ? 'Complete training'
              : sheet === 'award'
                ? 'Record award'
                : sheet === 'waive'
                  ? 'Waive requirement'
                  : ''
        }
      >
        {sheet === 'start' && activeRequirement ? (
          <p className="mb-4 text-sm text-secondary-label">
            Mark <span className="font-medium text-label">{activeRequirement.name}</span> as in
            progress?
          </p>
        ) : null}

        {sheet === 'complete' ? (
          <label className="mb-4 block text-sm text-secondary-label">
            Completed on
            <input
              type="date"
              value={completeDate}
              onChange={(event) => setCompleteDate(event.target.value)}
              className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
            />
          </label>
        ) : null}

        {sheet === 'award' ? (
          <>
            <label className="mb-4 block text-sm text-secondary-label">
              Awarded on
              <input
                type="date"
                value={awardDate}
                onChange={(event) => setAwardDate(event.target.value)}
                className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
              />
            </label>
            <label className="mb-4 block text-sm text-secondary-label">
              Certificate number (optional)
              <input
                value={certificateNumber}
                onChange={(event) => setCertificateNumber(event.target.value)}
                className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
              />
            </label>
            <AwardEvidenceField file={awardEvidenceFile} onFileChange={setAwardEvidenceFile} />
          </>
        ) : null}

        {sheet === 'waive' ? (
          <label className="mb-4 block text-sm text-secondary-label">
            Reason
            <textarea
              value={waiveReason}
              onChange={(event) => setWaiveReason(event.target.value)}
              rows={3}
              className="mt-1 w-full rounded-xl bg-elevated px-3 py-2 text-label ring-1 ring-separator/50"
            />
          </label>
        ) : null}

        <button
          type="button"
          onClick={() => void submitSheet()}
          className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white"
        >
          Confirm
        </button>
      </Sheet>
    </div>
  )
}

function MyRequirementActions({
  requirement,
  canWaive,
  onAction,
  onAcknowledge,
  onRequestRenewal,
}: {
  requirement: RequirementStatus
  canWaive: boolean
  onAction: (kind: ActionKind, requirement: RequirementStatus) => void
  onAcknowledge: (requirement: RequirementStatus) => void
  onRequestRenewal: (requirement: RequirementStatus) => void
}) {
  if (requirement.assignmentStatus === 'Waived') {
    return null
  }

  const actions: Array<{ label: string; kind?: ActionKind; helper?: string; onClick?: () => void }> = []

  if (requirement.kind === 'Certification') {
    if (requirement.status !== 'Compliant' && requirement.status !== 'ExpiringSoon') {
      actions.push({ label: 'Record award', kind: 'award' })
    }
    if (
      (requirement.status === 'ExpiringSoon' || requirement.status === 'Expired')
      && !requirement.pendingRenewalRequestId
    ) {
      actions.push({
        label: 'Request renewal',
        onClick: () => onRequestRenewal(requirement),
        helper: 'Ask your manager to renew before it expires',
      })
    }
    if (canWaive) {
      actions.push({
        label: 'Waive',
        kind: 'waive',
        helper: 'Excuses this requirement without completing it',
      })
    }
  }

  if (requirement.kind === 'Training') {
    if (requirement.assignmentStatus === 'NotStarted') {
      actions.push({ label: 'Start', kind: 'start' })
    }

    if (requirement.assignmentStatus !== 'Completed') {
      actions.push({ label: 'Mark complete', kind: 'complete' })
    }

    if (
      requirement.requiresAcknowledgement
      && requirement.assignmentStatus === 'Completed'
      && !requirement.acknowledgedOn
    ) {
      actions.push({
        label: 'Acknowledge',
        onClick: () => onAcknowledge(requirement),
        helper: 'Confirms you have read and understood this training',
      })
    }

    if (
      (requirement.status === 'ExpiringSoon' || requirement.status === 'Expired')
      && !requirement.pendingRenewalRequestId
    ) {
      actions.push({
        label: 'Request renewal',
        onClick: () => onRequestRenewal(requirement),
      })
    }
  }

  if (actions.length === 0) {
    return null
  }

  return (
    <>
      {actions.map((action) => (
        <div key={action.label} className="inline-flex flex-col">
          <button
            type="button"
            onClick={() => {
              if (action.onClick) {
                action.onClick()
              } else if (action.kind) {
                onAction(action.kind, requirement)
              }
            }}
            className="rounded-lg bg-accent-muted px-3 py-1.5 text-xs font-semibold text-accent"
          >
            {action.label}
          </button>
          {action.helper ? (
            <span className="mt-1 max-w-[12rem] text-[10px] leading-tight text-secondary-label">
              {action.helper}
            </span>
          ) : null}
        </div>
      ))}
      {requirement.pendingRenewalRequestId ? (
        <span className="text-[10px] text-secondary-label">Renewal pending approval</span>
      ) : null}
    </>
  )
}

function RequirementsSkeleton() {
  return (
    <InsetGroupedList>
      {Array.from({ length: 4 }).map((_, index) => (
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
