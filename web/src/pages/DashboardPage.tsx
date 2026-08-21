import { CheckCircle2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import type { RequirementKind } from '../api/dashboard'
import { useDashboard } from '../api/dashboard'
import { DashboardChartCard } from '../components/dashboard/DashboardChartCard'
import { LocationComplianceChart } from '../components/dashboard/LocationComplianceChart'
import { OpenGapsByKindChart } from '../components/dashboard/OpenGapsByKindChart'
import { RenewalHorizonChart } from '../components/dashboard/RenewalHorizonChart'
import { RequirementStatusPieChart } from '../components/dashboard/RequirementStatusPieChart'
import { NotifyEmployeeButton } from '../components/NotificationBell'
import { isManagerOrAdmin, useAuth } from '../contexts/AuthContext'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { KpiTile, StatusChip } from '../components/StatusChip'
import { cn } from '../lib/cn'

const KIND_LABELS: Record<RequirementKind, string> = {
  Certification: 'Certification',
  Training: 'Training',
  Skill: 'Skill',
}

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function KpiSkeleton() {
  return (
    <div className="animate-pulse rounded-2xl bg-grouped p-4 shadow-sm ring-1 ring-separator/40">
      <div className="h-4 w-28 rounded-md bg-separator/50" />
      <div className="mt-3 h-9 w-20 rounded-md bg-separator/50" />
    </div>
  )
}

function ChartSkeleton() {
  return (
    <div className="animate-pulse rounded-2xl bg-grouped p-5 shadow-sm ring-1 ring-separator/40">
      <div className="mb-4 h-4 w-40 rounded-md bg-separator/50" />
      <div className="h-[220px] rounded-xl bg-separator/30" />
    </div>
  )
}

function ListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <InsetGroupedList>
      {Array.from({ length: rows }).map((_, index) => (
        <div
          key={index}
          className="flex min-h-11 animate-pulse items-center gap-3 border-b border-separator/60 px-4 py-3 last:border-b-0"
        >
          <div className="min-w-0 flex-1 space-y-2">
            <div className="h-4 w-3/5 rounded-md bg-separator/50" />
            <div className="h-3 w-2/5 rounded-md bg-separator/40" />
          </div>
          <div className="h-6 w-20 rounded-full bg-separator/50" />
        </div>
      ))}
    </InsetGroupedList>
  )
}

function DashboardSkeleton() {
  return (
    <div className="space-y-8">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-6">
        {Array.from({ length: 6 }).map((_, index) => (
          <KpiSkeleton key={index} />
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <ChartSkeleton key={index} />
        ))}
      </div>

      <div className="grid gap-8 lg:grid-cols-2">
        <section className="space-y-2">
          <div className="h-3 w-32 animate-pulse rounded bg-separator/50 px-4" />
          <ListSkeleton rows={5} />
        </section>
        <section className="space-y-2">
          <div className="h-3 w-24 animate-pulse rounded bg-separator/50 px-4" />
          <ListSkeleton rows={5} />
        </section>
      </div>
    </div>
  )
}

function FeedEmptyState({
  title,
  description,
  positive = false,
}: {
  title: string
  description: string
  positive?: boolean
}) {
  return (
    <div
      className={cn(
        'mx-4 mb-4 mt-2 flex items-start gap-3 rounded-xl px-4 py-4',
        positive
          ? 'bg-[var(--color-status-compliant-bg)] ring-1 ring-[var(--color-status-compliant)]/20'
          : 'bg-elevated ring-1 ring-separator/40',
      )}
    >
      {positive ? (
        <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-[var(--color-status-compliant)]" />
      ) : null}
      <div>
        <p className="text-sm font-medium text-label">{title}</p>
        <p className="mt-1 text-sm text-secondary-label">{description}</p>
      </div>
    </div>
  )
}

export function DashboardPage() {
  const navigate = useNavigate()
  const { accessLevel } = useAuth()
  const canNotify = isManagerOrAdmin(accessLevel)
  const { data, isLoading, isError } = useDashboard()

  return (
    <div className="w-full">
      <LargeTitleHeader
        title="Dashboard"
        subtitle="Workforce readiness overview"
      />

      {isLoading ? <DashboardSkeleton /> : null}

      {isError ? (
        <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
          <p className="text-base font-medium text-label">Unable to load dashboard</p>
          <p className="mt-1 text-sm text-secondary-label">
            Check that the API is running and try again.
          </p>
        </div>
      ) : null}

      {data ? (
        <div className="space-y-8">
          <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-6">
            <KpiTile
              label="Total employees"
              value={data.totalEmployees}
              hint="Active crew in your scope"
              to="/employees"
            />
            <KpiTile
              label="Average readiness"
              value={`${data.overallCompliancePercent}%`}
              hint="Mean readiness score across your crew"
              to="/reports?report=readiness-summary"
            />
            <KpiTile
              label="Fully ready"
              value={data.employeesFullyReady}
              hint="All mandatory requirements met"
              to="/reports?report=compliance-leaders"
            />
            <KpiTile
              label="Fully ready rate"
              value={`${data.fullyReadyPercent}%`}
              hint="Share of employees who are fully ready"
              to="/reports?report=compliance-leaders"
            />
            <KpiTile
              label="Expiring in 60 days"
              value={data.expiringWithin60Days}
              hint="Renewals coming up"
              to="/expirations"
            />
            <KpiTile
              label="Expired / overdue"
              value={data.expiredOrOverdue}
              hint="Needs immediate attention"
              to="/reports?report=at-risk-employees"
            />
          </section>

          <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <DashboardChartCard
              title="Requirement status"
              subtitle="Health mix across all assigned requirements"
              className="md:col-span-1 xl:col-span-1"
              to="/reports?report=readiness-summary"
            >
              <RequirementStatusPieChart data={data.statusBreakdown} />
            </DashboardChartCard>

            <DashboardChartCard
              title="Compliance by location"
              subtitle="Average readiness per site"
              className="md:col-span-1 xl:col-span-1"
              to="/reports?report=location-scorecard"
            >
              <LocationComplianceChart data={data.byLocation} />
            </DashboardChartCard>

            <DashboardChartCard
              title="Renewal horizon"
              subtitle="Upcoming expirations in the next 90 days"
              className="md:col-span-1 xl:col-span-1"
              to="/expirations"
            >
              <RenewalHorizonChart data={data.renewalHorizon} />
            </DashboardChartCard>

            <DashboardChartCard
              title="Open gaps by type"
              subtitle="Outstanding items by requirement category"
              className="md:col-span-2 xl:col-span-1"
              to="/reports?report=compliance-gaps"
            >
              <OpenGapsByKindChart data={data.openGapsByKind} />
            </DashboardChartCard>
          </section>

          <div className="grid gap-8 lg:grid-cols-2">
            <InsetGroupedList title="Expiring soon">
              {data.expiringSoonFeed.length === 0 ? (
                <FeedEmptyState
                  positive
                  title="All renewals look good"
                  description="No certifications or training are expiring soon in your scope."
                />
              ) : (
                data.expiringSoonFeed.map((item) => (
                  <Row
                    key={`${item.employeeId}-${item.requirementName}-${item.effectiveDate}`}
                    label={item.employeeName}
                    description={`${item.requirementName} · ${item.locationName} · ${formatDate(item.effectiveDate)}`}
                    accessory={
                      <div className="flex items-center gap-2">
                        {canNotify ? (
                          <NotifyEmployeeButton
                            employeeId={item.employeeId}
                            employeeName={item.employeeName}
                            context={`${item.requirementName} expiring ${formatDate(item.effectiveDate)}`}
                          />
                        ) : null}
                        <StatusChip status={item.status} />
                      </div>
                    }
                    chevron
                    onClick={() => navigate(`/employees/${item.employeeId}`)}
                  />
                ))
              )}
            </InsetGroupedList>

            <InsetGroupedList title="Top gaps">
              {data.topGaps.length === 0 ? (
                <FeedEmptyState
                  positive
                  title="No outstanding gaps"
                  description="Everyone in scope is meeting their mandatory requirements."
                />
              ) : (
                data.topGaps.map((item) => (
                  <Row
                    key={`${item.employeeId}-${item.requirementName}-${item.status}`}
                    label={item.employeeName}
                    description={`${KIND_LABELS[item.kind]} · ${item.requirementName} · ${item.locationName}`}
                    accessory={
                      <div className="flex items-center gap-2">
                        {canNotify ? (
                          <NotifyEmployeeButton
                            employeeId={item.employeeId}
                            employeeName={item.employeeName}
                            context={`${item.requirementName} (${item.status})`}
                          />
                        ) : null}
                        <StatusChip status={item.status} />
                      </div>
                    }
                    chevron
                    onClick={() => navigate(`/employees/${item.employeeId}`)}
                  />
                ))
              )}
            </InsetGroupedList>
          </div>
        </div>
      ) : null}
    </div>
  )
}
