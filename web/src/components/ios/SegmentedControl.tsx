import { cn } from '../../lib/cn'

export function SegmentedControl<T extends string>({
  value,
  options,
  onChange,
  className,
}: {
  value: T
  options: Array<{ value: T; label: string }>
  onChange: (value: T) => void
  className?: string
}) {
  return (
    <div
      role="tablist"
      className={cn(
        'inline-flex rounded-xl bg-elevated p-1 ring-1 ring-separator/50',
        className,
      )}
    >
      {options.map((option) => {
        const selected = option.value === value
        return (
          <button
            key={option.value}
            type="button"
            role="tab"
            aria-selected={selected}
            onClick={() => onChange(option.value)}
            className={cn(
              'min-h-8 rounded-lg px-4 text-sm font-medium transition-[transform,opacity,background-color,color]',
              selected
                ? 'bg-grouped text-label shadow-sm'
                : 'text-secondary-label hover:text-label',
            )}
          >
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
