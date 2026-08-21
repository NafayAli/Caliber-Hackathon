import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useLogin } from '../api/auth'
import { getApiErrorMessage } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { getDefaultRoute } from '../components/ProtectedRoute'
import { fieldClassName } from '../components/catalogue/formFields'

export function LoginPage() {
  const navigate = useNavigate()
  const { isAuthenticated, accessLevel } = useAuth()
  const login = useLogin()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  if (isAuthenticated) {
    return <Navigate to={getDefaultRoute(accessLevel)} replace />
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    try {
      const user = await login.mutateAsync({ email, password })
      toast.success(`Welcome back, ${user.firstName}`)
      navigate(getDefaultRoute(user.accessLevel), { replace: true })
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-bg px-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <img src="/caliber-logo.svg" alt="Caliber" className="mx-auto mb-4 h-16 object-contain" />
          <h1 className="text-2xl font-semibold tracking-tight text-label">Sign in</h1>
          <p className="mt-1 text-sm text-secondary-label">Workforce readiness for equipment dealerships</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="rounded-2xl border border-separator/40 bg-grouped p-6 shadow-sm"
        >
          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-label">Email</span>
            <input
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={fieldClassName}
              placeholder="you@dealership.com"
            />
          </label>

          <label className="mb-6 block">
            <span className="mb-1 block text-sm font-medium text-label">Password</span>
            <input
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className={fieldClassName}
            />
          </label>

          <button
            type="submit"
            disabled={login.isPending}
            className="w-full rounded-xl bg-accent py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-50"
          >
            {login.isPending ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-secondary-label">
          New here?{' '}
          <Link to="/signup" className="font-medium text-accent hover:underline">
            Create an account
          </Link>
        </p>
      </div>
    </div>
  )
}
