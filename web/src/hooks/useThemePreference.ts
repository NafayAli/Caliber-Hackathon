import { useSyncExternalStore } from 'react'
import {
  getResolvedTheme,
  getStoredTheme,
  setStoredTheme,
  subscribeThemeChange,
  type ThemePreference,
} from '../hooks/useTheme'

export function useTheme() {
  const preference = useSyncExternalStore(
    (onStoreChange) => {
      const media = window.matchMedia('(prefers-color-scheme: dark)')
      const handler = () => onStoreChange()
      media.addEventListener('change', handler)
      window.addEventListener('storage', handler)
      const unsubscribeTheme = subscribeThemeChange(onStoreChange)
      return () => {
        media.removeEventListener('change', handler)
        window.removeEventListener('storage', handler)
        unsubscribeTheme()
      }
    },
    getStoredTheme,
    () => 'system' as ThemePreference,
  )

  const resolvedTheme = useSyncExternalStore(
    subscribeThemeChange,
    getResolvedTheme,
    () => 'light' as const,
  )

  return {
    preference,
    resolvedTheme,
    isDark: resolvedTheme === 'dark',
    setPreference: (next: ThemePreference) => setStoredTheme(next),
    toggle: () => {
      const isDark = getResolvedTheme() === 'dark'
      setStoredTheme(isDark ? 'light' : 'dark')
    },
  }
}
