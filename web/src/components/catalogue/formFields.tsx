import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export function Field({
  label,
  children,
  className,
}: {
  label: string
  children: ReactNode
  className?: string
}) {
  return (
    <label className={cn('block text-sm text-secondary-label', className)}>
      {label}
      <div className="mt-1">{children}</div>
    </label>
  )
}

export const fieldClassName =
  'min-h-11 w-full rounded-xl bg-elevated px-3 text-[15px] text-label ring-1 ring-separator/50 focus:outline-none focus:ring-accent'

export const textareaClassName =
  'w-full rounded-xl bg-elevated px-3 py-2 text-[15px] text-label ring-1 ring-separator/50 focus:outline-none focus:ring-accent'

export function DetailRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 border-b border-separator/60 py-3 last:border-b-0">
      <span className="text-sm text-secondary-label">{label}</span>
      <span className="text-right text-sm font-medium text-label">{value}</span>
    </div>
  )
}

export function BoolLabel({ value }: { value: boolean }) {
  return value ? 'Yes' : 'No'
}
