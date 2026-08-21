export type ThemePreference = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'caliber-theme'
const THEME_CHANGE_EVENT = 'caliber-theme-change'

function notifyThemeChange(): void {
  window.dispatchEvent(new Event(THEME_CHANGE_EVENT))
}

export function subscribeThemeChange(onStoreChange: () => void): () => void {
  const handler = () => onStoreChange()
  window.addEventListener(THEME_CHANGE_EVENT, handler)
  return () => window.removeEventListener(THEME_CHANGE_EVENT, handler)
}

export function getResolvedTheme(): 'light' | 'dark' {
  return document.documentElement.classList.contains('dark') ? 'dark' : 'light'
}

function resolveDark(preference: ThemePreference): boolean {
  if (preference === 'dark') {
    return true
  }

  if (preference === 'light') {
    return false
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

export function applyTheme(preference: ThemePreference): void {
  const root = document.documentElement
  root.classList.remove('light', 'dark')
  root.classList.add(resolveDark(preference) ? 'dark' : 'light')
  root.dataset.theme = preference
  notifyThemeChange()
}

export function getStoredTheme(): ThemePreference {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark' || stored === 'system') {
    return stored
  }

  return 'system'
}

export function setStoredTheme(preference: ThemePreference): void {
  localStorage.setItem(STORAGE_KEY, preference)
  applyTheme(preference)
}

export function initTheme(): void {
  applyTheme(getStoredTheme())

  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (getStoredTheme() === 'system') {
      applyTheme('system')
    }
  })
}
