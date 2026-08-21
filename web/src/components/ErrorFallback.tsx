import { Component, type ErrorInfo, type ReactNode } from 'react'
import { getApiErrorMessage } from '../api/client'

interface ErrorFallbackProps {
  error: Error
  resetErrorBoundary?: () => void
}

export function ErrorFallback({ error, resetErrorBoundary }: ErrorFallbackProps) {
  return (
    <div className="mx-auto flex min-h-[40vh] max-w-lg flex-col items-center justify-center gap-4 rounded-2xl border border-separator bg-grouped p-8 text-center shadow-sm">
      <h2 className="text-xl font-semibold text-label">Something went wrong</h2>
      <p className="text-sm text-secondary-label">{getApiErrorMessage(error)}</p>
      {resetErrorBoundary ? (
        <button
          type="button"
          className="rounded-full bg-accent px-5 py-2 text-sm font-medium text-white"
          onClick={resetErrorBoundary}
        >
          Try again
        </button>
      ) : null}
    </div>
  )
}

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: (error: Error, reset: () => void) => ReactNode
}

interface ErrorBoundaryState {
  error: Error | null
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Render error', error, info)
  }

  reset = (): void => {
    this.setState({ error: null })
  }

  render(): ReactNode {
    const { error } = this.state
    if (error) {
      if (this.props.fallback) {
        return this.props.fallback(error, this.reset)
      }

      return <ErrorFallback error={error} resetErrorBoundary={this.reset} />
    }

    return this.props.children
  }
}
