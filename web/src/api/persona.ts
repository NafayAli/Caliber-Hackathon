export const PERSONA_STORAGE_KEY = 'caliber-persona-id'

export function getPersonaId(): string {
  return localStorage.getItem(PERSONA_STORAGE_KEY) ?? ''
}

export function setPersonaId(id: number): void {
  localStorage.setItem(PERSONA_STORAGE_KEY, String(id))
}

export function clearPersonaId(): void {
  localStorage.removeItem(PERSONA_STORAGE_KEY)
}

export function hasPersonaId(): boolean {
  return getPersonaId().length > 0
}
