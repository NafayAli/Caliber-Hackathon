import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Toaster, toast } from 'sonner'
import { ApiError, getApiErrorMessage } from './api/client'
import App from './App.tsx'
import { ErrorBoundary } from './components/ErrorFallback.tsx'
import { OfflineBanner } from './components/OfflineBanner.tsx'
import { initTheme } from './hooks/useTheme'
import './index.css'
import './styles/reports.css'

initTheme()

function handleQueryError(error: unknown): void {
  if (error instanceof ApiError && error.status === 401) {
    return
  }

  if (error instanceof ApiError && error.status === 403) {
    toast.error(error.detail ?? error.title)
    return
  }

  console.error(error)
}

function handleMutationError(error: unknown): void {
  toast.error(getApiErrorMessage(error))
}

const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: handleQueryError,
  }),
  mutationCache: new MutationCache({
    onError: handleMutationError,
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ErrorBoundary>
        <OfflineBanner />
        <App />
        <Toaster richColors position="top-center" />
      </ErrorBoundary>
    </QueryClientProvider>
  </StrictMode>,
)
