import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export function InsetGroupedList({
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
      <div className="overflow-hidden rounded-2xl bg-accent-muted/15 shadow-sm ring-1 ring-separator/60">
        {children}
      </div>
    </section>
  )
}

export function Row({
  label,
  description,
  accessory,
  chevron = false,
  onClick,
  onMouseEnter,
  className,
}: {
  label: ReactNode
  description?: ReactNode
  accessory?: ReactNode
  chevron?: boolean
  onClick?: () => void
  onMouseEnter?: () => void
  className?: string
}) {
  if (onClick && accessory) {
    return (
      <div
        onMouseEnter={onMouseEnter}
        className={cn(
          'flex min-h-11 w-full items-center gap-2 border-b border-separator/60 px-4 py-3 last:border-b-0',
          className,
        )}
      >
        <button
          type="button"
          onClick={onClick}
          className="flex min-w-0 flex-1 items-center gap-3 text-left transition-opacity hover:opacity-80 active:opacity-60"
        >
          <div className="min-w-0 flex-1">
            <div className="truncate text-[15px] font-medium text-label">{label}</div>
            {description ? (
              <div className="truncate text-sm text-secondary-label">{description}</div>
            ) : null}
          </div>
          {chevron ? (
            <svg
              aria-hidden
              className="h-4 w-4 shrink-0 text-secondary-label"
              viewBox="0 0 20 20"
              fill="none"
            >
              <path
                d="M7.5 5l5 5-5 5"
                stroke="currentColor"
                strokeWidth="1.75"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          ) : null}
        </button>
        <div className="flex shrink-0 items-center gap-2">{accessory}</div>
      </div>
    )
  }

  const Component = onClick ? 'button' : 'div'

  return (
    <Component
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      onMouseEnter={onMouseEnter}
      className={cn(
        'flex min-h-11 w-full items-center gap-3 border-b border-separator/60 px-4 py-3 text-left last:border-b-0',
        onClick && 'transition-opacity hover:opacity-80 active:opacity-60',
        className,
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="truncate text-[15px] font-medium text-label">{label}</div>
        {description ? (
          <div className="truncate text-sm text-secondary-label">{description}</div>
        ) : null}
      </div>
      {accessory ? <div className="shrink-0">{accessory}</div> : null}
      {chevron ? (
        <svg
          aria-hidden
          className="h-4 w-4 shrink-0 text-secondary-label"
          viewBox="0 0 20 20"
          fill="none"
        >
          <path
            d="M7.5 5l5 5-5 5"
            stroke="currentColor"
            strokeWidth="1.75"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      ) : null}
    </Component>
  )
}
