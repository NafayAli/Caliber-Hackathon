import * as SwitchPrimitive from '@radix-ui/react-switch'
import { cn } from '../../lib/cn'

export function Switch({
  checked,
  onCheckedChange,
  label,
  className,
}: {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  label?: string
  className?: string
}) {
  return (
    <label className={cn('inline-flex items-center gap-3', className)}>
      {label ? <span className="text-sm text-label">{label}</span> : null}
      <SwitchPrimitive.Root
        checked={checked}
        onCheckedChange={onCheckedChange}
        className={cn(
          'relative h-7 w-12 rounded-full transition-[background-color,opacity]',
          'bg-separator data-[state=checked]:bg-accent',
        )}
      >
        <SwitchPrimitive.Thumb
          className={cn(
            'block h-6 w-6 translate-x-0.5 rounded-full bg-white shadow transition-transform',
            'data-[state=checked]:translate-x-[22px]',
          )}
        />
      </SwitchPrimitive.Root>
    </label>
  )
}
