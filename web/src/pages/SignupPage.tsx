import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useQuery } from '@tanstack/react-query'
import { type JobRoleItem } from '../api/catalogues'
import { apiPublic } from '../api/client'
import { useLocations, useRegister } from '../api/auth'
import { getApiErrorMessage } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { getDefaultRoute } from '../components/ProtectedRoute'
import { fieldClassName } from '../components/catalogue/formFields'

export function SignupPage() {
  const navigate = useNavigate()
  const { isAuthenticated, accessLevel } = useAuth()
  const register = useRegister()
  const { data: locations = [] } = useLocations()
  const { data: jobRoles = [] } = useQuery({
    queryKey: ['job-roles', 'public'],
    queryFn: () => apiPublic<JobRoleItem[]>('/api/job-roles'),
  })

  const [form, setForm] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    locationId: 0,
    jobRoleId: 0,
  })

  if (isAuthenticated) {
    return <Navigate to={getDefaultRoute(accessLevel)} replace />
  }

  function updateField<K extends keyof typeof form>(key: K, value: typeof form[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (form.password !== form.confirmPassword) {
      toast.error('Passwords do not match')
      return
    }
    if (!form.locationId || !form.jobRoleId) {
      toast.error('Please select a location and job role')
      return
    }

    try {
      const user = await register.mutateAsync(form)
      toast.success(`Welcome, ${user.firstName}!`)
      navigate(getDefaultRoute(user.accessLevel), { replace: true })
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-bg px-4 py-10">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <img src="/caliber-logo.svg" alt="Caliber" className="mx-auto mb-4 h-16 object-contain" />
          <h1 className="text-2xl font-semibold tracking-tight text-label">Create account</h1>
          <p className="mt-1 text-sm text-secondary-label">Join as a technician at your dealership</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="rounded-2xl border border-separator/40 bg-grouped p-6 shadow-sm"
        >
          <div className="mb-4 grid grid-cols-2 gap-3">
            <label>
              <span className="mb-1 block text-sm font-medium text-label">First name</span>
              <input
                required
                value={form.firstName}
                onChange={(e) => updateField('firstName', e.target.value)}
                className={fieldClassName}
              />
            </label>
            <label>
              <span className="mb-1 block text-sm font-medium text-label">Last name</span>
              <input
                required
                value={form.lastName}
                onChange={(e) => updateField('lastName', e.target.value)}
                className={fieldClassName}
              />
            </label>
          </div>

          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-label">Email</span>
            <input
              type="email"
              required
              value={form.email}
              onChange={(e) => updateField('email', e.target.value)}
              className={fieldClassName}
            />
          </label>

          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-label">Location</span>
            <select
              required
              value={form.locationId || ''}
              onChange={(e) => updateField('locationId', Number(e.target.value))}
              className={fieldClassName}
            >
              <option value="">Select location…</option>
              {locations.map((loc) => (
                <option key={loc.id} value={loc.id}>{loc.name}</option>
              ))}
            </select>
          </label>

          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-label">Job role</span>
            <select
              required
              value={form.jobRoleId || ''}
              onChange={(e) => updateField('jobRoleId', Number(e.target.value))}
              className={fieldClassName}
            >
              <option value="">Select role…</option>
              {jobRoles.map((role) => (
                <option key={role.id} value={role.id}>{role.name}</option>
              ))}
            </select>
          </label>

          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-label">Password</span>
            <input
              type="password"
              required
              minLength={6}
              value={form.password}
              onChange={(e) => updateField('password', e.target.value)}
              className={fieldClassName}
            />
          </label>

          <label className="mb-6 block">
            <span className="mb-1 block text-sm font-medium text-label">Confirm password</span>
            <input
              type="password"
              required
              minLength={6}
              value={form.confirmPassword}
              onChange={(e) => updateField('confirmPassword', e.target.value)}
              className={fieldClassName}
            />
          </label>

          <button
            type="submit"
            disabled={register.isPending}
            className="w-full rounded-xl bg-accent py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-50"
          >
            {register.isPending ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-secondary-label">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-accent hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  )
}
