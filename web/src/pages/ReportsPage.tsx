import { BarChart3, CalendarClock, Grid3X3, MapPin, ShieldAlert, Trophy, Users } from 'lucide-react'
import { useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { ReportKind } from '../api/reports'
import { REPORT_META } from '../api/reports'
import { ReportViewer } from '../components/reports/ReportViewer'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { cn } from '../lib/cn'
import '../styles/reports.css'

const REPORT_CARDS: Array<{
  kind: ReportKind
  icon: typeof BarChart3
  accent: string
}> = [
  {
    kind: 'readiness-summary',
    icon: Users,
    accent: 'bg-accent-muted text-accent',
  },
  {
    kind: 'at-risk-employees',
    icon: ShieldAlert,
    accent: 'bg-status-danger-bg text-status-danger',
  },
  {
    kind: 'compliance-leaders',
    icon: Trophy,
    accent: 'bg-status-compliant-bg text-status-compliant',
  },
  {
    kind: 'location-scorecard',
    icon: MapPin,
    accent: 'bg-status-progress-bg text-status-progress',
  },
  {
    kind: 'expiration-schedule',
    icon: CalendarClock,
    accent: 'bg-status-expiring-bg text-status-expiring',
  },
  {
    kind: 'compliance-gaps',
    icon: BarChart3,
    accent: 'bg-status-danger-bg text-status-danger',
  },
  {
    kind: 'skills-matrix',
    icon: Grid3X3,
    accent: 'bg-status-progress-bg text-status-progress',
  },
]

function isReportKind(value: string | null): value is ReportKind {
  return value != null && value in REPORT_META
}

export function ReportsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const reportParam = searchParams.get('report')

  const activeReport = useMemo(
    () => (isReportKind(reportParam) ? reportParam : null),
    [reportParam],
  )

  if (activeReport) {
    return (
      <ReportViewer
        kind={activeReport}
        onClose={() => setSearchParams({})}
      />
    )
  }

  return (
    <div className="mx-auto max-w-5xl">
      <LargeTitleHeader
        title="Reports"
        subtitle="Workforce readiness insights"
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {REPORT_CARDS.map(({ kind, icon: Icon, accent }) => {
          const meta = REPORT_META[kind]
          return (
            <button
              key={kind}
              type="button"
              onClick={() => setSearchParams({ report: kind })}
              className={cn(
                'rounded-2xl bg-grouped p-5 text-left shadow-sm ring-1 ring-separator/40',
                'transition hover:ring-accent/40 active:scale-[0.99]',
              )}
            >
              <div className={cn('mb-4 inline-flex rounded-xl p-3', accent)}>
                <Icon className="h-6 w-6" strokeWidth={1.5} />
              </div>
              <h2 className="text-lg font-semibold text-label">{meta.title}</h2>
              <p className="mt-1 text-sm text-secondary-label">{meta.description}</p>
            </button>
          )
        })}
      </div>
    </div>
  )
}
