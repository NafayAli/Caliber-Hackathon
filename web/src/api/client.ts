import { getPersonaId } from './persona'
import type { ProblemDetails } from './types'

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly traceId?: string
  readonly detail?: string
  readonly fieldErrors?: Record<string, string[]>

  constructor(
    status: number,
    title: string,
    options?: {
      traceId?: string
      detail?: string
      fieldErrors?: Record<string, string[]>
    },
  ) {
    super(options?.detail ?? title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.traceId = options?.traceId
    this.detail = options?.detail
    this.fieldErrors = options?.fieldErrors
  }
}

function buildHeaders(init?: RequestInit): Headers {
  const headers = new Headers(init?.headers)

  if (!(init?.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const personaId = getPersonaId()
  if (personaId) {
    headers.set('X-Persona-Id', personaId)
  }

  return headers
}

async function parseProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return {
      title: response.statusText,
      status: response.status,
    }
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: buildHeaders(init),
  })

  if (!response.ok) {
    const problem = await parseProblem(response)
    throw new ApiError(response.status, problem.title ?? response.statusText, {
      traceId: problem.traceId,
      detail: problem.detail,
      fieldErrors: problem.errors,
    })
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('Content-Type') ?? ''
  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  return undefined as T
}

/** Fetch without requiring a persona header (e.g. `/api/personas`). */
export async function apiPublic<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!(init?.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers,
  })

  if (!response.ok) {
    const problem = await parseProblem(response)
    throw new ApiError(response.status, problem.title ?? response.statusText, {
      traceId: problem.traceId,
      detail: problem.detail,
      fieldErrors: problem.errors,
    })
  }

  return (await response.json()) as T
}

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.detail ?? error.title
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong.'
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}
