import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import {
  MODULE_KEYS,
  useAppSettings,
  useModuleAccessMatrix,
  useMyModules,
  useUpdateAppSettings,
  useUpdateModuleAccess,
} from '../api/settings'
import { getApiErrorMessage } from '../api/client'
import type { AccessLevel } from '../api/types'
import { fieldClassName } from '../components/catalogue/formFields'
import { FormSection } from '../components/ios/FormSection'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'
import { SegmentedControl } from '../components/ios/SegmentedControl'
import { isAdmin, isManagerOrAdmin, useAuth } from '../contexts/AuthContext'
import { cn } from '../lib/cn'
import { SIDEBAR_THEME_PRESETS, type SidebarThemeKey } from '../lib/sidebarThemes'

type SettingsTab = 'general' | 'modules'

const ACCESS_LEVELS: AccessLevel[] = ['Technician', 'Manager', 'Admin']

const MODULE_LABELS: Record<string, string> = {
  [MODULE_KEYS.Dashboard]: 'Dashboard',
  [MODULE_KEYS.Employees]: 'Employees',
  [MODULE_KEYS.Users]: 'Users',
  [MODULE_KEYS.Certifications]: 'Certifications',
  [MODULE_KEYS.Training]: 'Training',
  [MODULE_KEYS.Skills]: 'Skills',
  [MODULE_KEYS.Roles]: 'Roles',
  [MODULE_KEYS.Expirations]: 'Expirations',
  [MODULE_KEYS.Reports]: 'Reports',
  [MODULE_KEYS.Settings]: 'Settings',
  [MODULE_KEYS.MyRequirements]: 'My requirements',
  [MODULE_KEYS.Profile]: 'Profile',
  [MODULE_KEYS.About]: 'About',
}

export function SettingsPage() {
  const { accessLevel } = useAuth()
  const [tab, setTab] = useState<SettingsTab>('general')
  const { data: settings } = useAppSettings()
  const { data: matrix } = useModuleAccessMatrix()
  const updateSettings = useUpdateAppSettings()
  const updateModule = useUpdateModuleAccess()

  const [form, setForm] = useState({
    applicationName: '',
    organizationName: '',
    contactEmail: '',
    supportPhone: '',
    tagline: '',
    sidebarThemeKey: 'charcoal' as SidebarThemeKey,
  })

  useEffect(() => {
    if (settings) {
      setForm({
        applicationName: settings.applicationName,
        organizationName: settings.organizationName,
        contactEmail: settings.contactEmail ?? '',
        supportPhone: settings.supportPhone ?? '',
        tagline: settings.tagline ?? '',
        sidebarThemeKey: (settings.sidebarThemeKey as SidebarThemeKey) || 'charcoal',
      })
    }
  }, [settings])

  if (!isManagerOrAdmin(accessLevel)) {
    return (
      <div className="mx-auto max-w-3xl">
        <p className="text-secondary-label">You do not have access to settings.</p>
      </div>
    )
  }

  async function saveGeneral(e: React.FormEvent) {
    e.preventDefault()
    try {
      await updateSettings.mutateAsync({
        applicationName: form.applicationName.trim(),
        organizationName: form.organizationName.trim(),
        contactEmail: form.contactEmail.trim() || undefined,
        supportPhone: form.supportPhone.trim() || undefined,
        tagline: form.tagline.trim() || undefined,
        sidebarThemeKey: form.sidebarThemeKey,
      })
      toast.success('Settings saved')
    } catch (error) {
      toast.error(getApiErrorMessage(error) || 'Unable to save settings')
    }
  }

  async function toggleModule(level: AccessLevel, moduleKey: string, isEnabled: boolean) {
    try {
      await updateModule.mutateAsync({ accessLevel: level, moduleKey, isEnabled })
    } catch {
      toast.error('Unable to update module access')
    }
  }

  const modules = matrix?.modules ?? []

  return (
    <div className="mx-auto max-w-4xl">
      <LargeTitleHeader title="Settings" subtitle="Organization branding and module access" />

      <SegmentedControl
        value={tab}
        onChange={(value) => setTab(value as SettingsTab)}
        options={[
          { value: 'general', label: 'General' },
          { value: 'modules', label: 'Module access' },
        ]}
        className="mb-6"
      />

      {tab === 'general' ? (
        <form onSubmit={(e) => void saveGeneral(e)}>
          <FormSection title="Organization">
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-label">Application name</span>
              <input
                required
                value={form.applicationName}
                onChange={(e) => setForm((f) => ({ ...f, applicationName: e.target.value }))}
                className={fieldClassName}
              />
            </label>
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-label">Organization name</span>
              <input
                required
                value={form.organizationName}
                onChange={(e) => setForm((f) => ({ ...f, organizationName: e.target.value }))}
                className={fieldClassName}
              />
            </label>
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-label">Contact email</span>
              <input
                type="email"
                value={form.contactEmail}
                onChange={(e) => setForm((f) => ({ ...f, contactEmail: e.target.value }))}
                className={fieldClassName}
              />
            </label>
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-label">Support phone</span>
              <input
                value={form.supportPhone}
                onChange={(e) => setForm((f) => ({ ...f, supportPhone: e.target.value }))}
                className={fieldClassName}
              />
            </label>
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-label">Tagline</span>
              <input
                value={form.tagline}
                onChange={(e) => setForm((f) => ({ ...f, tagline: e.target.value }))}
                className={fieldClassName}
              />
            </label>

            <div>
              <span className="mb-2 block text-sm font-medium text-label">Navigation panel color</span>
              <p className="mb-3 text-sm text-secondary-label">
                Choose a sidebar color for all users. Teal accents on content stay unchanged.
              </p>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                {SIDEBAR_THEME_PRESETS.map((preset) => {
                  const selected = form.sidebarThemeKey === preset.key
                  return (
                    <button
                      key={preset.key}
                      type="button"
                      onClick={() => setForm((f) => ({ ...f, sidebarThemeKey: preset.key }))}
                      className={cn(
                        'flex min-h-11 items-center gap-3 rounded-xl px-3 py-2 text-left ring-1 transition-[ring-color,transform]',
                        selected
                          ? 'ring-2 ring-accent'
                          : 'ring-separator/50 hover:ring-separator',
                      )}
                    >
                      <span
                        className="h-8 w-8 shrink-0 rounded-lg ring-1 ring-black/10"
                        style={{ backgroundColor: preset.swatch }}
                        aria-hidden
                      />
                      <span className="text-sm font-medium text-label">{preset.label}</span>
                    </button>
                  )
                })}
              </div>
            </div>

            <button
              type="submit"
              disabled={updateSettings.isPending}
              className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-50"
            >
              Save settings
            </button>
          </FormSection>
        </form>
      ) : null}

      {tab === 'modules' ? (
        <FormSection title="Role module access">
          <p className="text-sm text-secondary-label">
            Toggle which modules each role can see in navigation.
            {!isAdmin(accessLevel) ? ' Only administrators can change admin access.' : null}
          </p>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[36rem] text-left text-sm">
              <thead>
                <tr className="border-b border-separator/60">
                  <th className="py-2 pr-4 font-medium text-label">Module</th>
                  {ACCESS_LEVELS.map((level) => (
                    <th key={level} className="px-2 py-2 text-center font-medium text-label">
                      {level}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {Object.values(MODULE_KEYS).map((moduleKey) => (
                  <tr key={moduleKey} className="border-b border-separator/40">
                    <td className="py-2 pr-4 text-label">{MODULE_LABELS[moduleKey] ?? moduleKey}</td>
                    {ACCESS_LEVELS.map((level) => {
                      const row = modules.find(
                        (m) => m.moduleKey === moduleKey && m.accessLevel === level,
                      )
                      const checked = row?.isEnabled ?? false
                      const disabled =
                        !isAdmin(accessLevel)
                        && (level === 'Admin' || moduleKey === MODULE_KEYS.Settings)
                      return (
                        <td key={level} className="px-2 py-2 text-center">
                          <input
                            type="checkbox"
                            checked={checked}
                            disabled={disabled || updateModule.isPending}
                            onChange={(e) =>
                              void toggleModule(level, moduleKey, e.target.checked)
                            }
                            aria-label={`${MODULE_LABELS[moduleKey]} for ${level}`}
                          />
                        </td>
                      )
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </FormSection>
      ) : null}
    </div>
  )
}

/** Hook for nav gating — re-export convenience */
export { useMyModules }
