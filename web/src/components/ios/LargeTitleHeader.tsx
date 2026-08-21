import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export function LargeTitleHeader({
  title,
  subtitle,
  actions,
  className,
}: {
  title: string
  subtitle?: string
  actions?: ReactNode
  className?: string
}) {
  return (
    <header className={cn('mb-6 flex items-end justify-between gap-4', className)}>
      <div>
        <h1 className="text-[34px] font-bold leading-tight tracking-tight text-label">{title}</h1>
        {subtitle ? <p className="mt-1 text-base text-secondary-label">{subtitle}</p> : null}
      </div>
      {actions ? <div className="shrink-0 pb-1">{actions}</div> : null}
    </header>
  )
}
