import { Link } from 'react-router-dom'
import { cn } from '../lib/cn'

export type ReadinessStatus =
  | 'Compliant'
  | 'ExpiringSoon'
  | 'Expired'
  | 'Overdue'
  | 'InProgress'
  | 'Missing'
  | 'Waived'

const STATUS_LABELS: Record<ReadinessStatus, string> = {
  Compliant: 'Compliant',
  ExpiringSoon: 'Expiring soon',
  Expired: 'Expired',
  Overdue: 'Overdue',
  InProgress: 'In progress',
  Missing: 'Missing',
  Waived: 'Waived',
}

const STATUS_STYLES: Record<ReadinessStatus, string> = {
  Compliant: 'bg-[var(--color-status-compliant-bg)] text-[var(--color-status-compliant)]',
  ExpiringSoon: 'bg-[var(--color-status-expiring-bg)] text-[var(--color-status-expiring)]',
  Expired: 'bg-[var(--color-status-danger-bg)] text-[var(--color-status-danger)]',
  Overdue: 'bg-[var(--color-status-danger-bg)] text-[var(--color-status-danger)]',
  InProgress: 'bg-[var(--color-status-progress-bg)] text-[var(--color-status-progress)]',
  Missing: 'bg-[var(--color-status-missing-bg)] text-[var(--color-status-missing)]',
  Waived: 'bg-[var(--color-status-waived-bg)] text-[var(--color-status-waived)]',
}

export function StatusChip({
  status,
  className,
}: {
  status: ReadinessStatus
  className?: string
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold',
        STATUS_STYLES[status],
        className,
      )}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}

export function ReadinessBar({
  percent,
  className,
}: {
  percent: number
  className?: string
}) {
  const clamped = Math.max(0, Math.min(100, percent))

  return (
    <div className={cn('h-2 w-full overflow-hidden rounded-full bg-separator/40', className)}>
      <div
        className="h-full rounded-full bg-accent transition-[width,opacity] duration-300"
        style={{ width: `${clamped}%` }}
      />
    </div>
  )
}

export function KpiTile({
  label,
  value,
  hint,
  className,
  onClick,
  to,
}: {
  label: string
  value: string | number
  hint?: string
  className?: string
  onClick?: () => void
  to?: string
}) {
  const interactive = onClick != null || to != null
  const tileClassName = cn(
    'rounded-2xl border-l-4 border-accent bg-accent-muted/30 p-4 shadow-sm ring-1 ring-separator/40',
    interactive && 'cursor-pointer transition hover:ring-2 hover:ring-accent/30 active:scale-[0.99]',
    className,
  )

  const content = (
    <>
      <div className="text-sm font-medium text-secondary-label">{label}</div>
      <div className="mt-2 text-3xl font-semibold tracking-tight text-label">{value}</div>
      {hint ? <div className="mt-1 text-xs text-secondary-label">{hint}</div> : null}
    </>
  )

  if (to) {
    return (
      <Link to={to} className={cn(tileClassName, 'block no-underline')}>
        {content}
      </Link>
    )
  }

  if (onClick) {
    return (
      <button type="button" onClick={onClick} className={cn(tileClassName, 'w-full text-left')}>
        {content}
      </button>
    )
  }

  return <div className={tileClassName}>{content}</div>
}
