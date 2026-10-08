// Types returned by the API and small helpers shared by the components.

export type Project = {
  id: number
  name: string
  createdAt: string
  // Inputs of the requirements prompt.
  systemName: string // with article: "el sistema ReservaSalas"
  language: 'es' | 'en'
  glossary: string | null
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

// Skipped: a run that was not launched (the arbiter, when A or B did not end well).
export type RunState = 'Queued' | 'Running' | 'Done' | 'Blocked' | 'Failed' | 'TimedOut' | 'HeartbeatLost' | 'Skipped'

// A, B and, in the v0 phases, ARB (the arbiter).
export type Slot = 'A' | 'B' | 'ARB'

export type Run = {
  id: string
  projectId: number
  jobId: string | null
  slot: Slot | null
  lotNumber: number | null
  lotTotal: number | null
  promptVersion: string | null
  promptSha256: string | null
  claudeCodeVersion: string | null
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

export type Job = {
  id: string
  projectId: number
  kind: string // "catalog" (v1.4, experimental) or a v0 phase: "questions", "catalog-v0", "backlog"
  createdAt: string
  finishedAt: string | null
}

// The job kinds of the v0 phases (A ‖ B → arbiter).
export const PHASE_JOB_KINDS = ['questions', 'catalog-v0', 'backlog']

export function isPhaseJob(job: Job): boolean {
  return PHASE_JOB_KINDS.includes(job.kind)
}

export const JOB_KIND_NAMES: Record<string, string> = {
  questions: 'Ronda de preguntas',
  'catalog-v0': 'Catálogo',
  backlog: 'Backlog',
  mockup: 'Mockup',
  catalog: 'Catálogo v1.4 (experimental)',
}

// GET /jobs/{id}/result: the output of a v0 phase job that counts, and where
// it comes from (ARB, or A or B alone without cross-check; null if it failed).
export type PhaseResult = {
  fuente: Slot | null
  verificacion_cruzada: boolean
  mensaje: string
  salida: unknown
}

// GET /projects/{id}/flow: the state of each step of the project.
export type FlowStep = 'Documents' | 'Questions' | 'Catalog' | 'Backlog' | 'Mockup'
export type StepState = 'Blocked' | 'Available' | 'InProgress' | 'Draft' | 'Validated'

export type StepStatus = {
  step: FlowStep
  state: StepState
  outdated: boolean
  canLaunch: boolean
  reason: string | null
  detail: string | null
}

export type FlowView = { steps: StepStatus[] }

// A job with all its runs: slot A and slot B, plus their retries.
export type JobView = {
  job: Job
  runs: Run[]
}

// Metrics of the validator (validar_catalogo.py), with its Spanish names.
// Ratios go from 0 to 1; null when there is nothing to measure.
export type CatalogMetrics = {
  requisitos: number
  conformidad_sintactica: number | null
  calidad_redaccion: number | null
  coherencia_decision: number | null
  trazabilidad: number | null
  fidelidad_cifras: number | null
  requisitos_sin_errores: number | null
  citas_literales: string // "12/13"
  cobertura_fragmentos: string // "14/14"
  proporcion_master: number | null
  ramas: Record<string, number>
  preguntas_abiertas: number
  terminos_dominio: number
  autoverificacion_coincide: boolean
}

export type CatalogReport = {
  valid: boolean
  errors: string[]
  warnings: string[]
  metrics: CatalogMetrics | null
}

// The parts of the catalog (schema 1.2, snake_case) that the web shows.
export type SourceTrace = { fragment: string; quote: string | null }

export type Requirement = {
  id: string
  source_trace: SourceTrace[]
  obligation: string // SHALL, SHOULD, MAY...
  kind: string
  notation: 'EARS' | 'MASTeR'
  ears_pattern: string | null
  master_template: string | null
  text: string | null // rendered by the platform from the slots
  open_question_ids: string[]
}

export type OpenQuestion = {
  id: string
  type: string
  question: string
  related_requirements: string[]
  source_trace: SourceTrace[]
}

export type Catalog = {
  system: string
  language: string
  requirements: Requirement[]
  open_questions: OpenQuestion[]
  discarded_statements: {
    fragment: string
    reason: string
    note: string | null
  }[]
}

// GET /jobs/{id}/catalogs/{slot}: the Done lots of one model, merged.
export type SlotCatalog = {
  slot: 'A' | 'B'
  model: string
  lotTotal: number
  includedLots: number[]
  failedLots: number[]
  pendingLots: number[]
  report: CatalogReport | null
  catalog: Catalog | null
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
