import { useNavigate } from 'react-router-dom'
import { useExpirations } from '../api/expirations'
import type { RequirementKind } from '../api/dashboard'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { StatusChip } from '../components/StatusChip'

const KIND_LABELS: Record<RequirementKind, string> = {
  Certification: 'Certification',
  Training: 'Training',
  Skill: 'Skill',
}

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

export function ExpirationsPage() {
  const navigate = useNavigate()
  const { data, isLoading, isError } = useExpirations()

  const totalItems = data?.buckets.reduce((sum, bucket) => sum + bucket.items.length, 0) ?? 0

  return (
    <div className="mx-auto max-w-3xl">
      <LargeTitleHeader
        title="Expirations"
        subtitle={
          data
            ? `${totalItems} renewal${totalItems === 1 ? '' : 's'} in the next 90 days`
            : 'Upcoming renewals'
        }
      />

      {isLoading ? <BucketsSkeleton /> : null}

      {isError ? (
        <div className="rounded-2xl bg-grouped p-6 text-center shadow-sm ring-1 ring-separator/40">
          <p className="font-medium text-label">Unable to load expirations</p>
        </div>
      ) : null}

      {data ? (
        <div className="space-y-8">
          {data.buckets.map((bucket) => (
            <InsetGroupedList
              key={bucket.days}
              title={`${bucket.label} (${bucket.items.length})`}
            >
              {bucket.items.length === 0 ? (
                <div className="px-4 py-6 text-center text-sm text-secondary-label">
                  Nothing expiring in this window.
                </div>
              ) : (
                bucket.items.map((item) => (
                  <Row
                    key={`${item.employeeId}-${item.requirementName}-${item.effectiveDate}`}
                    label={item.employeeName}
                    description={`${KIND_LABELS[item.kind]} · ${item.requirementName} · ${item.locationName} · ${formatDate(item.effectiveDate)}`}
                    accessory={<StatusChip status={item.status} />}
                    chevron
                    onClick={() => navigate(`/employees/${item.employeeId}`)}
                  />
                ))
              )}
            </InsetGroupedList>
          ))}
        </div>
      ) : null}
    </div>
  )
}

function BucketsSkeleton() {
  return (
    <div className="space-y-8">
      {Array.from({ length: 3 }).map((_, bucketIndex) => (
        <section key={bucketIndex} className="space-y-2">
          <div className="mx-4 h-3 w-28 animate-pulse rounded bg-separator/50" />
          <InsetGroupedList>
            {Array.from({ length: 3 }).map((__, rowIndex) => (
              <div
                key={rowIndex}
                className="flex animate-pulse items-center gap-3 border-b border-separator/60 px-4 py-3 last:border-b-0"
              >
                <div className="min-w-0 flex-1 space-y-2">
                  <div className="h-4 w-2/5 rounded bg-separator/50" />
                  <div className="h-3 w-3/5 rounded bg-separator/40" />
                </div>
                <div className="h-6 w-20 rounded-full bg-separator/50" />
              </div>
            ))}
          </InsetGroupedList>
        </section>
      ))}
    </div>
  )
}
