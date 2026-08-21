import { Printer, RefreshCw, X } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import type { ReportKind } from '../../api/reports'
import {
  REPORT_META,
  useAtRiskEmployeesReport,
  useComplianceGapsReport,
  useComplianceLeadersReport,
  useExpirationScheduleReport,
  useLocationScorecardReport,
  useReadinessSummaryReport,
  useSkillsMatrixReport,
} from '../../api/reports'
import { useAppSettings } from '../../api/settings'
import { useAuth } from '../../contexts/AuthContext'
import { ReadinessBar, StatusChip } from '../StatusChip'
import { cn } from '../../lib/cn'
import '../../styles/reports.css'

interface ReportViewerProps {
  kind: ReportKind
  onClose: () => void
}

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function ReportPrintHeader({ organizationName, title }: { organizationName: string; title: string }) {
  return (
    <div className="report-print-header" role="doc-title">
      <div className="report-print-header-org">{organizationName}</div>
      <div className="report-print-header-title">{title}</div>
    </div>
  )
}

function ReportPrintFooter({
  printDate,
  userLabel,
}: {
  printDate: string
  userLabel: string
}) {
  const year = new Date().getFullYear()
  return (
    <>
      <footer className="report-print-page-footer">
        <div className="report-print-page-footer-left">Printed: {printDate}</div>
        <div className="report-print-page-footer-center">{userLabel}</div>
        <div className="report-print-page-footer-right" aria-label="Page number" />
      </footer>
      <div className="report-print-copyright">
        © {year} Caliber Reporting — All rights reserved
      </div>
    </>
  )
}

export function ReportViewer({ kind, onClose }: ReportViewerProps) {
  const meta = REPORT_META[kind]
  const { user } = useAuth()
  const { data: settings } = useAppSettings()
  const [search, setSearch] = useState('')
  const [locationFilter, setLocationFilter] = useState('')
  const [printDate, setPrintDate] = useState('')
  const printDateRef = useRef('')

  const readiness = useReadinessSummaryReport()
  const expiration = useExpirationScheduleReport()
  const gaps = useComplianceGapsReport()
  const matrix = useSkillsMatrixReport()
  const atRisk = useAtRiskEmployeesReport()
  const leaders = useComplianceLeadersReport()
  const locationScorecard = useLocationScorecardReport()

  const query =
    kind === 'readiness-summary'
      ? readiness
      : kind === 'expiration-schedule'
        ? expiration
        : kind === 'compliance-gaps'
          ? gaps
          : kind === 'skills-matrix'
            ? matrix
            : kind === 'at-risk-employees'
              ? atRisk
              : kind === 'compliance-leaders'
                ? leaders
                : locationScorecard

  const locations = useMemo(() => {
    if (kind === 'readiness-summary' && readiness.data) {
      return [...new Set(readiness.data.employees.map((e) => e.locationName))].sort()
    }
    if (kind === 'compliance-gaps' && gaps.data) {
      return [...new Set(gaps.data.gaps.map((g) => g.locationName))].sort()
    }
    if (kind === 'skills-matrix' && matrix.data) {
      return [...new Set(matrix.data.rows.map((r) => r.locationName))].sort()
    }
    if (kind === 'at-risk-employees' && atRisk.data) {
      return [...new Set(atRisk.data.employees.map((e) => e.locationName))].sort()
    }
    if (kind === 'compliance-leaders' && leaders.data) {
      return [...new Set(leaders.data.leaders.map((l) => l.locationName))].sort()
    }
    if (kind === 'location-scorecard' && locationScorecard.data) {
      return locationScorecard.data.locations.map((l) => l.locationName).sort()
    }
    if (kind === 'expiration-schedule' && expiration.data) {
      return [
        ...new Set(
          expiration.data.buckets.flatMap((b) => b.items.map((i) => i.locationName)),
        ),
      ].sort()
    }
    return []
  }, [kind, readiness.data, expiration.data, gaps.data, matrix.data, atRisk.data, leaders.data, locationScorecard.data])

  const term = search.trim().toLowerCase()
  const organizationName = settings?.organizationName ?? 'Constellation Dealer'
  const userLabel = user ? `${user.displayName} · ${user.jobRoleName}` : '—'

  function handlePrint() {
    const stamped = new Date().toLocaleString()
    printDateRef.current = stamped
    setPrintDate(stamped)
    window.setTimeout(() => window.print(), 50)
  }

  const footerDate = printDate || printDateRef.current || new Date().toLocaleString()

  return (
    <div className="report-print-root space-y-4">
      <div className="no-print flex items-start justify-between gap-4">
        <div>
          <p className="text-sm text-secondary-label">{meta.description}</p>
        </div>
        <div className="flex shrink-0 gap-2">
          <button
            type="button"
            onClick={() => void query.refetch()}
            disabled={query.isFetching}
            className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-elevated px-4 text-sm font-semibold text-label ring-1 ring-separator/50 disabled:opacity-50"
          >
            <RefreshCw className={cn('h-4 w-4', query.isFetching && 'animate-spin')} />
            Refresh
          </button>
          <button
            type="button"
            onClick={handlePrint}
            className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-accent px-4 text-sm font-semibold text-white"
          >
            <Printer className="h-4 w-4" />
            Export PDF
          </button>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close report"
            className="inline-flex min-h-10 items-center justify-center rounded-xl bg-elevated px-3 ring-1 ring-separator/50"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div className="no-print flex flex-wrap gap-2">
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search…"
          className="min-h-9 min-w-[12rem] flex-1 rounded-lg bg-elevated px-3 text-sm text-label ring-1 ring-separator/50 focus:outline-none focus:ring-accent"
        />
        {locations.length > 1 ? (
          <select
            value={locationFilter}
            onChange={(event) => setLocationFilter(event.target.value)}
            className="min-h-9 rounded-lg bg-elevated px-3 text-sm text-label ring-1 ring-separator/50"
          >
            <option value="">All locations</option>
            {locations.map((location) => (
              <option key={location} value={location}>
                {location}
              </option>
            ))}
          </select>
        ) : null}
      </div>

      {query.isLoading ? (
        <div className="rounded-2xl bg-grouped p-8 text-center text-sm text-secondary-label">
          Loading report…
        </div>
      ) : null}

      {query.isError ? (
        <div className="rounded-2xl bg-grouped p-8 text-center text-sm text-status-danger">
          Unable to load report.
        </div>
      ) : null}

      {query.data ? (
        <div className="report-print-surface overflow-x-auto rounded-2xl bg-grouped p-4 shadow-sm ring-1 ring-separator/40">
          <ReportPrintHeader organizationName={organizationName} title={meta.title} />
          {kind === 'readiness-summary' ? (
            <ReadinessSummaryView
              data={readiness.data!}
              term={term}
              locationFilter={locationFilter}
            />
          ) : null}
          {kind === 'expiration-schedule' ? (
            <ExpirationScheduleView
              data={expiration.data!}
              term={term}
              locationFilter={locationFilter}
            />
          ) : null}
          {kind === 'compliance-gaps' ? (
            <ComplianceGapsView data={gaps.data!} term={term} locationFilter={locationFilter} />
          ) : null}
          {kind === 'skills-matrix' ? (
            <SkillsMatrixView data={matrix.data!} term={term} locationFilter={locationFilter} />
          ) : null}
          {kind === 'at-risk-employees' ? (
            <AtRiskEmployeesView data={atRisk.data!} term={term} locationFilter={locationFilter} />
          ) : null}
          {kind === 'compliance-leaders' ? (
            <ComplianceLeadersView data={leaders.data!} term={term} locationFilter={locationFilter} />
          ) : null}
          {kind === 'location-scorecard' ? (
            <LocationScorecardView data={locationScorecard.data!} term={term} locationFilter={locationFilter} />
          ) : null}
          <ReportPrintFooter printDate={footerDate} userLabel={userLabel} />
        </div>
      ) : null}
    </div>
  )
}

function ReadinessSummaryView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useReadinessSummaryReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.employees.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return (
      row.employeeName.toLowerCase().includes(term)
      || row.locationName.toLowerCase().includes(term)
    )
  })

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3 print:grid-cols-3">
        <Kpi label="Overall compliance" value={`${data.overallCompliancePercent}%`} />
        <Kpi label="Total employees" value={String(data.totalEmployees)} />
        <Kpi label="Fully ready" value={String(data.employeesFullyReady)} />
      </div>
      <table className="report-print-table w-full text-left text-sm">
        <thead>
          <tr>
            <th className="py-2 pr-4">Employee</th>
            <th className="py-2 pr-4">Location</th>
            <th className="py-2 pr-4">Readiness</th>
            <th className="py-2 pr-4">Gaps</th>
            <th className="py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.employeeId} className="border-b border-separator/40">
              <td className="py-2 pr-4 font-medium">{row.employeeName}</td>
              <td className="report-cell-muted py-2 pr-4">{row.locationName}</td>
              <td className="py-2 pr-4">
                <div className="min-w-[8rem]">
                  <ReadinessBar percent={row.readinessPercent} />
                  <span className="text-xs tabular-nums text-secondary-label">{row.readinessPercent}%</span>
                </div>
              </td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.gapCount}</td>
              <td className="py-2">
                <StatusChip status={row.worstStatus} className="report-status-chip" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function ExpirationScheduleView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useExpirationScheduleReport>['data']>
  term: string
  locationFilter: string
}) {
  return (
    <div className="space-y-6">
      <Kpi label="Total expiring" value={String(data.totalExpiring)} />
      {data.buckets.map((bucket) => {
        const items = bucket.items.filter((item) => {
          if (locationFilter && item.locationName !== locationFilter) return false
          if (!term) return true
          return (
            item.employeeName.toLowerCase().includes(term)
            || item.requirementName.toLowerCase().includes(term)
            || item.locationName.toLowerCase().includes(term)
          )
        })
        if (items.length === 0) return null
        return (
          <section key={bucket.days}>
            <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-label">
              {bucket.label}
            </h3>
            <table className="report-print-table w-full text-left text-sm">
              <thead>
                <tr>
                  <th className="py-2 pr-4">Employee</th>
                  <th className="py-2 pr-4">Requirement</th>
                  <th className="py-2 pr-4">Location</th>
                  <th className="py-2 pr-4">Effective</th>
                  <th className="py-2">Status</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item, index) => (
                  <tr key={`${item.employeeId}-${item.requirementName}-${index}`}>
                    <td className="py-2 pr-4 font-medium">{item.employeeName}</td>
                    <td className="py-2 pr-4">{item.requirementName}</td>
                    <td className="report-cell-muted py-2 pr-4">{item.locationName}</td>
                    <td className="py-2 pr-4">{formatDate(item.effectiveDate)}</td>
                    <td className="py-2">
                      <StatusChip status={item.status} className="report-status-chip" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )
      })}
    </div>
  )
}

function ComplianceGapsView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useComplianceGapsReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.gaps.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return (
      row.employeeName.toLowerCase().includes(term)
      || row.requirementName.toLowerCase().includes(term)
      || row.locationName.toLowerCase().includes(term)
    )
  })

  return (
    <div className="space-y-4">
      <Kpi label="Total gaps" value={String(data.totalGaps)} />
      <table className="report-print-table w-full text-left text-sm">
        <thead>
          <tr>
            <th className="py-2 pr-4">Employee</th>
            <th className="py-2 pr-4">Requirement</th>
            <th className="py-2 pr-4">Kind</th>
            <th className="py-2 pr-4">Location</th>
            <th className="py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={`${row.employeeId}-${row.requirementName}-${index}`}>
              <td className="py-2 pr-4 font-medium">{row.employeeName}</td>
              <td className="py-2 pr-4">{row.requirementName}</td>
              <td className="report-cell-muted py-2 pr-4">{row.kind}</td>
              <td className="report-cell-muted py-2 pr-4">{row.locationName}</td>
              <td className="py-2">
                <StatusChip status={row.status} className="report-status-chip" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function SkillsMatrixView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useSkillsMatrixReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.rows.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return (
      row.employeeName.toLowerCase().includes(term)
      || row.locationName.toLowerCase().includes(term)
    )
  })

  if (data.skills.length === 0) {
    return <p className="text-sm text-label">No active skills in catalogue.</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="report-print-table w-full min-w-[40rem] text-left text-xs">
        <thead>
          <tr>
            <th className="sticky left-0 bg-grouped py-2 pr-3">Employee</th>
            {data.skills.map((skill) => (
              <th
                key={skill.skillId}
                className="max-w-[6rem] truncate px-1 py-2"
                title={skill.skillName}
              >
                {skill.skillName}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.employeeId}>
              <td className="sticky left-0 bg-grouped py-2 pr-3 font-medium">
                <div>{row.employeeName}</div>
                <div className="report-cell-muted text-[10px]">{row.locationName}</div>
              </td>
              {row.cells.map((cell) => (
                <td
                  key={cell.skillId}
                  className={cn(
                    'px-1 py-2 text-center tabular-nums',
                    !cell.proficiencyLevel && 'report-cell-muted',
                  )}
                >
                  {cell.proficiencyLevel?.slice(0, 3) ?? '—'}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function AtRiskEmployeesView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useAtRiskEmployeesReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.employees.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return (
      row.employeeName.toLowerCase().includes(term)
      || row.locationName.toLowerCase().includes(term)
      || row.topGapName.toLowerCase().includes(term)
    )
  })

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3 print:grid-cols-3">
        <Kpi label="Total at-risk" value={String(data.totalAtRisk)} />
        <Kpi label="Critical" value={String(data.criticalCount)} />
        <Kpi label="Avg readiness" value={`${data.avgReadinessPercent}%`} />
      </div>
      <table className="report-print-table w-full text-left text-sm">
        <thead>
          <tr>
            <th className="py-2 pr-4">Employee</th>
            <th className="py-2 pr-4">Location</th>
            <th className="py-2 pr-4">Readiness</th>
            <th className="py-2 pr-4">Expired</th>
            <th className="py-2 pr-4">Overdue</th>
            <th className="py-2 pr-4">Missing</th>
            <th className="py-2 pr-4">Top gap</th>
            <th className="py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.employeeId} className="border-b border-separator/40">
              <td className="py-2 pr-4 font-medium text-label">{row.employeeName}</td>
              <td className="report-cell-muted py-2 pr-4">{row.locationName}</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.readinessPercent}%</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.expiredCount}</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.overdueCount}</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.missingCount}</td>
              <td className="py-2 pr-4 text-label">{row.topGapName || '—'}</td>
              <td className="py-2">
                <StatusChip status={row.worstStatus} className="report-status-chip" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function ComplianceLeadersView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useComplianceLeadersReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.leaders.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return (
      row.employeeName.toLowerCase().includes(term)
      || row.locationName.toLowerCase().includes(term)
      || row.tier.toLowerCase().includes(term)
    )
  })

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-4 print:grid-cols-4">
        <Kpi label="Fully ready" value={String(data.fullyReadyCount)} />
        <Kpi label="Gold tier" value={String(data.goldCount)} />
        <Kpi label="Silver tier" value={String(data.silverCount)} />
        <Kpi label="Fully ready rate" value={`${data.workforceReadyPercent}%`} />
      </div>
      <table className="report-print-table w-full text-left text-sm">
        <thead>
          <tr>
            <th className="py-2 pr-4">Employee</th>
            <th className="py-2 pr-4">Location</th>
            <th className="py-2 pr-4">Tier</th>
            <th className="py-2 pr-4">Readiness</th>
            <th className="py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.employeeId} className="border-b border-separator/40">
              <td className="py-2 pr-4 font-medium text-label">{row.employeeName}</td>
              <td className="report-cell-muted py-2 pr-4">{row.locationName}</td>
              <td className="py-2 pr-4 font-semibold text-label">{row.tier}</td>
              <td className="py-2 pr-4">
                <div className="min-w-[8rem]">
                  <ReadinessBar percent={row.readinessPercent} />
                  <span className="text-xs tabular-nums text-secondary-label">{row.readinessPercent}%</span>
                </div>
              </td>
              <td className="py-2">
                <StatusChip status={row.worstStatus} className="report-status-chip" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function LocationScorecardView({
  data,
  term,
  locationFilter,
}: {
  data: NonNullable<ReturnType<typeof useLocationScorecardReport>['data']>
  term: string
  locationFilter: string
}) {
  const rows = data.locations.filter((row) => {
    if (locationFilter && row.locationName !== locationFilter) return false
    if (!term) return true
    return row.locationName.toLowerCase().includes(term)
  })

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3 print:grid-cols-3">
        <Kpi label="Org compliance" value={`${data.orgCompliancePercent}%`} />
        <Kpi label="Top location" value={data.topLocationName ?? '—'} />
        <Kpi label="Needs attention" value={data.bottomLocationName ?? '—'} />
      </div>
      <table className="report-print-table w-full text-left text-sm">
        <thead>
          <tr>
            <th className="py-2 pr-4">Rank</th>
            <th className="py-2 pr-4">Location</th>
            <th className="py-2 pr-4">Employees</th>
            <th className="py-2 pr-4">Compliance</th>
            <th className="py-2 pr-4">Fully ready</th>
            <th className="py-2 pr-4">At-risk</th>
            <th className="py-2">Expiring soon</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.locationId} className="border-b border-separator/40">
              <td className="py-2 pr-4 tabular-nums font-semibold text-label">#{row.rank}</td>
              <td className="py-2 pr-4 font-medium text-label">{row.locationName}</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.employeeCount}</td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.compliancePercent}%</td>
              <td className="py-2 pr-4 tabular-nums text-label">
                {row.fullyReadyCount} ({row.fullyReadyPercent}%)
              </td>
              <td className="py-2 pr-4 tabular-nums text-label">{row.atRiskCount}</td>
              <td className="py-2 tabular-nums text-label">{row.expiringSoonCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <EmptyFilter /> : null}
    </div>
  )
}

function Kpi({ label, value }: { label: string; value: string }) {
  return (
    <div className="report-kpi">
      <div className="report-kpi-label">{label}</div>
      <div className="report-kpi-value">{value}</div>
    </div>
  )
}

function EmptyFilter() {
  return (
    <p className="py-6 text-center text-sm text-label">
      No rows match your filters.
    </p>
  )
}
