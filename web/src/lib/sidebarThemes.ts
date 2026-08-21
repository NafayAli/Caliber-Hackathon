export const SIDEBAR_THEME_KEYS = {
  charcoal: 'charcoal',
  forest: 'forest',
  slate: 'slate',
  plum: 'plum',
  espresso: 'espresso',
  deepTeal: 'deepTeal',
  wine: 'wine',
  graphite: 'graphite',
  mustard: 'mustard',
  chocolate: 'chocolate',
  orange: 'orange',
} as const

export type SidebarThemeKey = (typeof SIDEBAR_THEME_KEYS)[keyof typeof SIDEBAR_THEME_KEYS]

export interface SidebarThemePreset {
  key: SidebarThemeKey
  label: string
  swatch: string
  light: string
  dark: string
}

/** Non-blue navigation panel presets that pair with the teal accent. */
export const SIDEBAR_THEME_PRESETS: SidebarThemePreset[] = [
  {
    key: 'charcoal',
    label: 'Charcoal',
    swatch: '#1f2937',
    light: '#1f2937',
    dark: '#111827',
  },
  {
    key: 'graphite',
    label: 'Graphite',
    swatch: '#292524',
    light: '#292524',
    dark: '#1c1917',
  },
  {
    key: 'slate',
    label: 'Slate',
    swatch: '#374151',
    light: '#374151',
    dark: '#1f2937',
  },
  {
    key: 'forest',
    label: 'Forest',
    swatch: '#14532d',
    light: '#14532d',
    dark: '#052e16',
  },
  {
    key: 'deepTeal',
    label: 'Deep teal',
    swatch: '#0f766e',
    light: '#0f766e',
    dark: '#115e59',
  },
  {
    key: 'plum',
    label: 'Plum',
    swatch: '#553c9a',
    light: '#553c9a',
    dark: '#44337a',
  },
  {
    key: 'espresso',
    label: 'Espresso',
    swatch: '#44403c',
    light: '#44403c',
    dark: '#292524',
  },
  {
    key: 'wine',
    label: 'Wine',
    swatch: '#7f1d1d',
    light: '#7f1d1d',
    dark: '#450a0a',
  },
  {
    key: 'mustard',
    label: 'Mustard',
    swatch: '#a16207',
    light: '#a16207',
    dark: '#713f12',
  },
  {
    key: 'chocolate',
    label: 'Chocolate',
    swatch: '#78350f',
    light: '#78350f',
    dark: '#451a03',
  },
  {
    key: 'orange',
    label: 'Orange',
    swatch: '#c2410c',
    light: '#c2410c',
    dark: '#7c2d12',
  },
]

const presetByKey = new Map(SIDEBAR_THEME_PRESETS.map((preset) => [preset.key, preset]))

export function resolveSidebarTheme(key: string | undefined | null): SidebarThemePreset {
  const preset = presetByKey.get(key as SidebarThemeKey)
  return preset ?? presetByKey.get('charcoal')!
}

export function sidebarThemeStyle(
  key: string | undefined | null,
  isDark: boolean,
): { '--color-sidebar': string; backgroundColor: string } {
  const preset = resolveSidebarTheme(key)
  const color = isDark ? preset.dark : preset.light
  return {
    '--color-sidebar': color,
    backgroundColor: color,
  }
}
