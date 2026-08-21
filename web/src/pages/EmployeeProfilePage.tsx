import { ArrowLeft, Eye, Plus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useDirectRenew } from '../api/renewals'
import {
  employeeKeys,
  useAssignCertification,
  useAssignSkill,
  useAssignTraining,
  useCompleteTraining,
  useDeleteEvidence,
  useEmployee,
  useRecordAward,
  useUpdateTrainingProgress,
  useVerifyEvidence,
  useWaiveRequirement,
  type EmployeeProfile,
  type EvidenceItem,
  type ProficiencyLevel,
  type RequirementStatus,
} from '../api/employees'
import {
  useCertificationCatalogue,
  useSkillsCatalogue,
  useTrainingCatalogue,
} from '../api/catalogues'
import {
  AwardEvidenceField,
  EvidencePreviewModal,
  formatFileSize,
  uploadEvidenceFile,
} from '../components/EvidenceUploader'
import { Avatar } from '../components/ios/Avatar'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { SegmentedControl } from '../components/ios/SegmentedControl'
import { Sheet } from '../components/ios/Sheet'
import { ReadinessBar, StatusChip } from '../components/StatusChip'
import { usePersona } from '../contexts/PersonaContext'
import { cn } from '../lib/cn'

type ProfileTab = 'requirements' | 'skills'
type ActionKind =
  | 'assign-cert'
  | 'assign-training'
  | 'assign-skill'
  | 'award'
  | 'waive'
  | 'start'
  | 'complete'

const PROFICIENCY_LEVELS: ProficiencyLevel[] = ['Beginner', 'Intermediate', 'Advanced', 'Expert']

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

function ProfileSkeleton() {
  return (
    <div className="animate-pulse space-y-6">
      <div className="flex items-center gap-4">
        <div className="h-14 w-14 rounded-full bg-separator/50" />
        <div className="space-y-2">
          <div className="h-6 w-48 rounded bg-separator/50" />
          <div className="h-4 w-32 rounded bg-separator/40" />
        </div>
      </div>
      <div className="h-2 w-full rounded-full bg-separator/40" />
    </div>
  )
}

export function EmployeeProfilePage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { personaId } = usePersona()
  const employeeId = Number(id)
  const { data: profile, isLoading, isError } = useEmployee(employeeId)

  const [tab, setTab] = useState<ProfileTab>('requirements')
  const [sheet, setSheet] = useState<ActionKind | null>(null)
  const [activeRequirement, setActiveRequirement] = useState<RequirementStatus | null>(null)
  const [previewEvidence, setPreviewEvidence] = useState<EvidenceItem | null>(null)
  const [awardEvidenceFile, setAwardEvidenceFile] = useState<File | null>(null)

  const assignCert = useAssignCertification(employeeId)
  const assignTraining = useAssignTraining(employeeId)
  const assignSkill = useAssignSkill(employeeId)
  const recordAward = useRecordAward(employeeId)
  const waive = useWaiveRequirement(employeeId)
  const completeTraining = useCompleteTraining(employeeId)
  const startTraining = useUpdateTrainingProgress(employeeId)
  const verifyEvidence = useVerifyEvidence(employeeId)
  const deleteEvidence = useDeleteEvidence(employeeId)
  const directRenew = useDirectRenew()

  const { data: certifications = [] } = useCertificationCatalogue()
  const { data: trainingPrograms = [] } = useTrainingCatalogue()
  const { data: skills = [] } = useSkillsCatalogue()

  const [selectedCatalogueId, setSelectedCatalogueId] = useState<number | ''>('')
  const [proficiencyLevel, setProficiencyLevel] = useState<ProficiencyLevel>('Beginner')
  const [skillNotes, setSkillNotes] = useState('')
  const [assessedOn, setAssessedOn] = useState(todayIso())
  const [awardDate, setAwardDate] = useState(todayIso())
  const [certificateNumber, setCertificateNumber] = useState('')
  const [waiveReason, setWaiveReason] = useState('')
  const [completeDate, setCompleteDate] = useState(todayIso())
  const [score, setScore] = useState('')

  const evidenceByCertId = useMemo(() => {
    const map = new Map<number, EvidenceItem>()
    for (const item of profile?.evidence ?? []) {
      if (item.employeeCertificationId != null) {
        map.set(item.employeeCertificationId, item)
      }
    }
    return map
  }, [profile?.evidence])

  function invalidateProfile() {
    void queryClient.invalidateQueries({ queryKey: employeeKeys.profile(personaId, employeeId) })
  }

  async function handleDirectRenew(requirement: RequirementStatus) {
    try {
      await directRenew.mutateAsync({
        kind: requirement.kind,
        assignmentId: requirement.sourceId,
      })
      toast.success('Requirement renewed')
      invalidateProfile()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Renewal failed')
    }
  }

  function openAction(kind: ActionKind, requirement?: RequirementStatus) {
    setActiveRequirement(requirement ?? null)
    setSelectedCatalogueId('')
    setProficiencyLevel('Beginner')
    setSkillNotes('')
    setAssessedOn(todayIso())
    setAwardDate(todayIso())
    setCertificateNumber('')
    setWaiveReason('')
    setCompleteDate(todayIso())
    setScore('')
    setAwardEvidenceFile(null)
    setSheet(kind)
  }

  function closeSheet() {
    setSheet(null)
    setActiveRequirement(null)
    setAwardEvidenceFile(null)
  }

  async function submitSheet() {
    try {
      if (sheet === 'assign-cert' && selectedCatalogueId !== '') {
        await assignCert.mutateAsync({ certificationId: selectedCatalogueId })
        toast.success('Certification assigned')
      } else if (sheet === 'assign-training' && selectedCatalogueId !== '') {
        await assignTraining.mutateAsync({ trainingProgramId: selectedCatalogueId })
        toast.success('Training assigned')
      } else if (sheet === 'assign-skill' && selectedCatalogueId !== '') {
        await assignSkill.mutateAsync({
          skillId: selectedCatalogueId,
          proficiencyLevel,
          assessedOn,
          notes: skillNotes.trim() || undefined,
        })
        toast.success('Skill assigned')
      } else if (sheet === 'award' && activeRequirement) {
        await recordAward.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          awardedOn: awardDate,
          certificateNumber: certificateNumber || undefined,
          rowVersion: activeRequirement.rowVersion,
        })
        if (awardEvidenceFile) {
          await uploadEvidenceFile(profile!.id, awardEvidenceFile, {
            employeeCertificationId: activeRequirement.sourceId,
            evidenceType: 'Certificate',
          })
          toast.success('Award recorded with evidence')
        } else {
          toast.success('Award recorded')
        }
        invalidateProfile()
      } else if (sheet === 'waive' && activeRequirement) {
        await waive.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          reason: waiveReason,
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Requirement waived')
      } else if (sheet === 'start' && activeRequirement) {
        await startTraining.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          status: 'InProgress',
          startedOn: todayIso(),
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Training started')
      } else if (sheet === 'complete' && activeRequirement) {
        await completeTraining.mutateAsync({
          assignmentId: activeRequirement.sourceId,
          completedOn: completeDate,
          score: score ? Number(score) : undefined,
          rowVersion: activeRequirement.rowVersion,
        })
        toast.success('Training completed')
      }
      closeSheet()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Action failed')
    }
  }

  const tabOptions = useMemo(
    () => [
      { value: 'requirements' as const, label: 'Requirements' },
      { value: 'skills' as const, label: 'Skills' },
    ],
    [],
  )

  if (!Number.isFinite(employeeId) || employeeId <= 0) {
    return (
      <div className="mx-auto max-w-3xl">
        <p className="text-secondary-label">Invalid employee.</p>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-3xl">
      <button
        type="button"
        onClick={() => navigate('/employees')}
        className="mb-4 inline-flex items-center gap-1 text-sm font-medium text-accent"
      >
        <ArrowLeft className="h-4 w-4" />
        Employees
      </button>

      {isLoading ? <ProfileSkeleton /> : null}

      {isError ? (
        <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
          <p className="font-medium text-label">Unable to load profile</p>
        </div>
      ) : null}

      {profile ? (
        <>
          <header className="mb-6 flex items-start gap-4">
            <Avatar name={profile.fullName} size="lg" />
            <div className="min-w-0 flex-1">
              <h1 className="text-[28px] font-bold leading-tight text-label">{profile.fullName}</h1>
              <p className="mt-1 text-secondary-label">
                {profile.jobRole} · {profile.location}
              </p>
              <p className="text-sm text-secondary-label">{profile.email}</p>
              <div className="mt-3 space-y-1">
                <ReadinessBar percent={profile.readinessPercent} />
                <p className="text-sm tabular-nums text-secondary-label">
                  {profile.readinessPercent}% readiness
                </p>
              </div>
            </div>
          </header>

          <SegmentedControl
            value={tab}
            options={tabOptions}
            onChange={(value) => setTab(value as ProfileTab)}
            className="mb-6"
          />

          {tab === 'requirements' ? (
            <RequirementsTab
              profile={profile}
              evidenceByCertId={evidenceByCertId}
              onAction={openAction}
              onPreviewEvidence={setPreviewEvidence}
              onRenew={(requirement) => void handleDirectRenew(requirement)}
            />
          ) : null}

          {tab === 'skills' ? <SkillsTab profile={profile} onAction={openAction} /> : null}
        </>
      ) : null}

      <EvidencePreviewModal
        open={previewEvidence != null}
        onOpenChange={(open) => {
          if (!open) setPreviewEvidence(null)
        }}
        item={previewEvidence}
        footer={
          previewEvidence ? (
            <div className="flex flex-wrap gap-3 pt-2">
              {!previewEvidence.isVerified ? (
                <button
                  type="button"
                  onClick={() => {
                    verifyEvidence.mutate(
                      { evidenceId: previewEvidence.id },
                      {
                        onSuccess: () => {
                          toast.success('Evidence verified')
                          setPreviewEvidence(null)
                          invalidateProfile()
                        },
                      },
                    )
                  }}
                  className="rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-white"
                >
                  Verify
                </button>
              ) : null}
              <button
                type="button"
                onClick={() => {
                  deleteEvidence.mutate(previewEvidence.id, {
                    onSuccess: () => {
                      toast.success('Evidence deleted')
                      setPreviewEvidence(null)
                      invalidateProfile()
                    },
                  })
                }}
                className="rounded-lg bg-elevated px-4 py-2 text-sm font-semibold text-[var(--color-status-danger)] ring-1 ring-separator/50"
              >
                Delete
              </button>
            </div>
          ) : null
        }
      />

      <Sheet
        open={sheet != null}
        onOpenChange={(open) => {
          if (!open) closeSheet()
        }}
        title={sheetTitle(sheet)}
      >
        <div className="space-y-4">
          {sheet === 'assign-cert' ? (
            <select
              value={selectedCatalogueId}
              onChange={(event) =>
                setSelectedCatalogueId(event.target.value ? Number(event.target.value) : '')
              }
              className="min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
            >
              <option value="">Select certification</option>
              {certifications.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} ({item.code})
                </option>
              ))}
            </select>
          ) : null}

          {sheet === 'assign-training' ? (
            <select
              value={selectedCatalogueId}
              onChange={(event) =>
                setSelectedCatalogueId(event.target.value ? Number(event.target.value) : '')
              }
              className="min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
            >
              <option value="">Select training program</option>
              {trainingPrograms.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} ({item.code})
                </option>
              ))}
            </select>
          ) : null}

          {sheet === 'assign-skill' ? (
            <>
              <select
                value={selectedCatalogueId}
                onChange={(event) =>
                  setSelectedCatalogueId(event.target.value ? Number(event.target.value) : '')
                }
                className="min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
              >
                <option value="">Select skill</option>
                {skills
                  .filter((item) => item.isActive)
                  .map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name} ({item.category})
                    </option>
                  ))}
              </select>
              <label className="block text-sm text-secondary-label">
                Proficiency
                <select
                  value={proficiencyLevel}
                  onChange={(event) => setProficiencyLevel(event.target.value as ProficiencyLevel)}
                  className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
                >
                  {PROFICIENCY_LEVELS.map((level) => (
                    <option key={level} value={level}>
                      {level}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block text-sm text-secondary-label">
                Assessed on
                <input
                  type="date"
                  value={assessedOn}
                  onChange={(event) => setAssessedOn(event.target.value)}
                  className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
                />
              </label>
              <label className="block text-sm text-secondary-label">
                Notes (optional)
                <textarea
                  value={skillNotes}
                  onChange={(event) => setSkillNotes(event.target.value)}
                  rows={3}
                  className="mt-1 w-full rounded-xl bg-elevated px-3 py-2 text-label ring-1 ring-separator/50"
                />
              </label>
            </>
          ) : null}

          {sheet === 'award' ? (
            <>
              <label className="block text-sm text-secondary-label">
                Awarded on
                <input
                  type="date"
                  value={awardDate}
                  onChange={(event) => setAwardDate(event.target.value)}
                  className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
                />
              </label>
              <label className="block text-sm text-secondary-label">
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
            <label className="block text-sm text-secondary-label">
              Reason
              <textarea
                value={waiveReason}
                onChange={(event) => setWaiveReason(event.target.value)}
                rows={3}
                className="mt-1 w-full rounded-xl bg-elevated px-3 py-2 text-label ring-1 ring-separator/50"
              />
            </label>
          ) : null}

          {sheet === 'complete' ? (
            <>
              <label className="block text-sm text-secondary-label">
                Completed on
                <input
                  type="date"
                  value={completeDate}
                  onChange={(event) => setCompleteDate(event.target.value)}
                  className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
                />
              </label>
              <label className="block text-sm text-secondary-label">
                Score (optional)
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={score}
                  onChange={(event) => setScore(event.target.value)}
                  className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
                />
              </label>
            </>
          ) : null}

          {sheet === 'start' && activeRequirement ? (
            <p className="text-sm text-secondary-label">
              Mark <span className="font-medium text-label">{activeRequirement.name}</span> as in
              progress?
            </p>
          ) : null}

          <button
            type="button"
            disabled={isSheetSubmitDisabled(sheet, selectedCatalogueId, waiveReason)}
            onClick={() => void submitSheet()}
            className={cn(
              'min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white',
              'disabled:opacity-40',
            )}
          >
            Confirm
          </button>
        </div>
      </Sheet>
    </div>
  )
}

function RequirementsTab({
  profile,
  evidenceByCertId,
  onAction,
  onPreviewEvidence,
  onRenew,
}: {
  profile: EmployeeProfile
  evidenceByCertId: Map<number, EvidenceItem>
  onAction: (kind: ActionKind, requirement?: RequirementStatus) => void
  onPreviewEvidence: (item: EvidenceItem) => void
  onRenew: (requirement: RequirementStatus) => void
}) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => onAction('assign-cert')}
          className="inline-flex min-h-9 items-center gap-1 rounded-lg bg-elevated px-3 text-sm font-medium text-label ring-1 ring-separator/50"
        >
          <Plus className="h-4 w-4" />
          Assign certification
        </button>
        <button
          type="button"
          onClick={() => onAction('assign-training')}
          className="inline-flex min-h-9 items-center gap-1 rounded-lg bg-elevated px-3 text-sm font-medium text-label ring-1 ring-separator/50"
        >
          <Plus className="h-4 w-4" />
          Assign training
        </button>
      </div>

      <InsetGroupedList title="Certifications & training">
        {profile.requirements.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-secondary-label">
            No requirements assigned yet.
          </div>
        ) : (
          profile.requirements.map((req) => {
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
                          ? `Evidence verified · ${formatFileSize(linkedEvidence.sizeBytes)}`
                          : `Evidence pending verification · ${formatFileSize(linkedEvidence.sizeBytes)}`}
                      </p>
                    ) : null}
                  </div>
                  <StatusChip status={req.status} />
                </div>
                <div className="mt-2 flex flex-wrap items-center gap-2">
                  {linkedEvidence ? (
                    <button
                      type="button"
                      onClick={() => onPreviewEvidence(linkedEvidence)}
                      className="inline-flex items-center gap-1 rounded-lg bg-elevated px-3 py-1.5 text-xs font-semibold text-label ring-1 ring-separator/50"
                    >
                      <Eye className="h-3.5 w-3.5" />
                      Preview evidence
                    </button>
                  ) : null}
                  <RequirementActions requirement={req} onAction={onAction} onRenew={onRenew} />
                </div>
              </div>
            )
          })
        )}
      </InsetGroupedList>
    </div>
  )
}

function RequirementActions({
  requirement,
  onAction,
  onRenew,
}: {
  requirement: RequirementStatus
  onAction: (kind: ActionKind, requirement: RequirementStatus) => void
  onRenew: (requirement: RequirementStatus) => void
}) {
  if (requirement.assignmentStatus === 'Waived') {
    return null
  }

  const actions: Array<{ label: string; kind?: ActionKind; helper?: string; onClick?: () => void }> = []

  if (requirement.kind === 'Certification') {
    if (requirement.status !== 'Compliant' && requirement.status !== 'ExpiringSoon') {
      actions.push({ label: 'Record award', kind: 'award' })
    }
    if (requirement.status === 'ExpiringSoon' || requirement.status === 'Expired') {
      actions.push({ label: 'Renew', onClick: () => onRenew(requirement) })
    }
    actions.push({
      label: 'Waive',
      kind: 'waive',
      helper: 'Excuses this requirement without completing it',
    })
  }

  if (requirement.kind === 'Training') {
    if (requirement.assignmentStatus === 'NotStarted') {
      actions.push({ label: 'Start', kind: 'start' })
    }
    if (requirement.assignmentStatus !== 'Completed') {
      actions.push({ label: 'Complete', kind: 'complete' })
    }
    if (requirement.status === 'ExpiringSoon' || requirement.status === 'Expired') {
      actions.push({ label: 'Renew', onClick: () => onRenew(requirement) })
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
    </>
  )
}

function SkillsTab({
  profile,
  onAction,
}: {
  profile: EmployeeProfile
  onAction: (kind: ActionKind) => void
}) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => onAction('assign-skill')}
          className="inline-flex min-h-9 items-center gap-1 rounded-lg bg-elevated px-3 text-sm font-medium text-label ring-1 ring-separator/50"
        >
          <Plus className="h-4 w-4" />
          Add skill
        </button>
      </div>

      <InsetGroupedList title="Skills">
        {profile.skills.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-secondary-label">No skills recorded.</div>
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
    </div>
  )
}

function sheetTitle(sheet: ActionKind | null): string {
  switch (sheet) {
    case 'assign-cert':
      return 'Assign certification'
    case 'assign-training':
      return 'Assign training'
    case 'assign-skill':
      return 'Assign skill'
    case 'award':
      return 'Record award'
    case 'waive':
      return 'Waive requirement'
    case 'start':
      return 'Start training'
    case 'complete':
      return 'Complete training'
    default:
      return ''
  }
}

function isSheetSubmitDisabled(
  sheet: ActionKind | null,
  selectedCatalogueId: number | '',
  waiveReason: string,
): boolean {
  if (sheet === 'assign-cert' || sheet === 'assign-training' || sheet === 'assign-skill') {
    return selectedCatalogueId === ''
  }
  if (sheet === 'waive') {
    return waiveReason.trim().length === 0
  }
  return sheet == null
}
