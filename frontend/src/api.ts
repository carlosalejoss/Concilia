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

export type RunState = 'Queued' | 'Running' | 'Done' | 'Blocked' | 'Failed' | 'TimedOut' | 'HeartbeatLost'

export type Run = {
  id: string
  projectId: number
  retryOfRunId: string | null
  attempt: number
  provider: string
  model: string
  state: RunState
  createdAt: string
  startedAt: string | null
  finishedAt: string | null
  verdictState: string | null
  verdictReason: string | null
  resultText: string | null
  inputTokens: number | null
  outputTokens: number | null
  numTurns: number | null
  errorMessage: string | null
}

export type RunDetails = {
  run: Run
  summary: string | null // content of output/resumen.md, if it was written
}

// One progress event, as sent by GET /runs/{id}/events.
export type RunEvent = {
  index: number
  kind: string
  text: string
  at: string
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
