// Types returned by the API and small helpers shared by the components.

export type Project = {
  id: number
  name: string
  createdAt: string
}

export type ProjectFile = {
  id: number
  projectId: number
  fileName: string
  storedName: string
  contentType: string
  sizeBytes: number
  sha256: string
  uploadedAt: string
  textStoredName: string | null // null: no text extracted for this type
}

// Reads the first validation message from an ASP.NET Core "problem details"
// response, or falls back to the HTTP status.
export async function errorMessage(response: Response): Promise<string> {
  // 413 comes from nginx or the API server itself, before our code runs.
  if (response.status === 413) {
    return 'El archivo es demasiado grande.'
  }
  try {
    const problem: { errors?: Record<string, string[]> } = await response.json()
    const first = Object.values(problem.errors ?? {})[0]?.[0]
    if (first) return first
  } catch {
    // The body was not JSON; use the status below.
  }
  return `HTTP ${response.status}`
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('es-ES')
}
