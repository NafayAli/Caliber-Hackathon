/** Shared API types. Run `npm run generate:api` to refresh from Swagger. */

export type AccessLevel = 'Technician' | 'Manager' | 'Admin'

export interface PersonaDto {
  id: number
  displayName: string
  accessLevel: AccessLevel
  jobRole: string
  location: string
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  errors?: Record<string, string[]>
}
