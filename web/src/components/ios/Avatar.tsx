import { cn } from '../../lib/cn'

function initials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

export function Avatar({
  name,
  src,
  size = 'md',
  className,
}: {
  name: string
  src?: string | null
  size?: 'sm' | 'md' | 'lg'
  className?: string
}) {
  const sizes = {
    sm: 'h-8 w-8 text-xs',
    md: 'h-10 w-10 text-sm',
    lg: 'h-14 w-14 text-lg',
  }

  if (src) {
    return (
      <img
        src={src}
        alt=""
        className={cn('rounded-full object-cover', sizes[size], className)}
      />
    )
  }

  return (
    <div
      aria-hidden
      className={cn(
        'inline-flex items-center justify-center rounded-full bg-accent-muted font-semibold text-accent',
        sizes[size],
        className,
      )}
    >
      {initials(name)}
    </div>
  )
}
