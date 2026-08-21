import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

/** Grouped card for forms — unlike InsetGroupedList, does not clip with overflow-hidden. */
export function FormSection({
  children,
  className,
  title,
}: {
  children: ReactNode
  className?: string
  title?: string
}) {
  return (
    <section className={cn('space-y-2', className)}>
      {title ? (
        <h3 className="px-4 text-xs font-semibold uppercase tracking-wide text-secondary-label">
          {title}
        </h3>
      ) : null}
      <div className="space-y-4 rounded-2xl border-l-4 border-accent/60 bg-accent-muted/20 p-4 shadow-sm ring-1 ring-separator/60">
        {children}
      </div>
    </section>
  )
}
