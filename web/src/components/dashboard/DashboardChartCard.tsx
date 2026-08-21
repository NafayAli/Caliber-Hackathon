import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export function DashboardChartCard({
  title,
  subtitle,
  children,
  className,
  onClick,
  to,
}: {
  title: string
  subtitle?: string
  children: ReactNode
  className?: string
  onClick?: () => void
  to?: string
}) {
  const interactive = onClick != null || to != null

  const sectionClassName = cn(
    'rounded-2xl bg-grouped p-4 shadow-sm ring-1 ring-separator/40 sm:p-5',
    interactive && 'cursor-pointer transition hover:ring-2 hover:ring-accent/30 active:scale-[0.99]',
    className,
  )

  const inner = (
    <>
      <div className="mb-4">
        <h2 className="text-base font-semibold text-label">{title}</h2>
        {subtitle ? <p className="mt-0.5 text-sm text-secondary-label">{subtitle}</p> : null}
      </div>
      {children}
    </>
  )

  if (to) {
    return (
      <Link to={to} className={cn(sectionClassName, 'block no-underline')}>
        {inner}
      </Link>
    )
  }

  if (onClick) {
    return (
      <button type="button" onClick={onClick} className={cn(sectionClassName, 'w-full text-left')}>
        {inner}
      </button>
    )
  }

  return <section className={sectionClassName}>{inner}</section>
}

export function ChartEmptyState({ message }: { message: string }) {
  return (
    <div className="flex h-[220px] items-center justify-center px-4 text-center text-sm text-secondary-label">
      {message}
    </div>
  )
}
