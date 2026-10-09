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

// A question of the project (GET /projects/{id}/questions), from a round.
export type QuestionStatus = 'Pending' | 'Answered' | 'Discarded'

export type Question = {
  id: number
  projectId: number
  jobId: string // the round where it was asked
  code: string // P-001, numbered by project
  text: string
  reason: string // which requirement cannot be written without the answer
  sources: string[]
  origin: 'A' | 'B' | 'ambas'
  arbiterNote: string | null
  status: QuestionStatus
  answer: string | null
  discardReason: string | null
  createdAt: string
  updatedAt: string | null
}

// A catalog or backlog of the project (GET /projects/{id}/deliverables/{kind}):
// the working copy, its errors (what keeps it from being validated) and warnings.
export type Deliverable<T> = {
  id: number
  kind: 'Catalog' | 'Backlog' | 'Mockup'
  status: 'Draft' | 'Validated'
  outdated: boolean
  jobId: string
  content: T
  errors: string[]
  warnings: string[]
  createdAt: string
  updatedAt: string | null
  validatedAt: string | null
}

// One requirement of the catalog v0 working copy (prompts/v0/esquemas/catalogo.schema.json
// plus the fields of the platform: origen, discrepancia, explicacion, revisada).
export type CatalogRequirement = {
  id: string
  tipo: 'RF' | 'RNF' | 'RA' | 'RT' | 'RI'
  notacion: 'EARS' | 'MASTeR'
  obligatoriedad: 'shall' | 'should' | 'may'
  texto: string
  fuentes: string[]
  preguntas: string[]
  origen?: string
  discrepancia?: boolean
  explicacion?: string
  revisada?: boolean
}

export type CatalogContent = { requisitos: CatalogRequirement[] }

// The backlog v0 working copy (prompts/v0/esquemas/backlog.schema.json): Épica →
// Historia → Criterios (Gherkin) and Tareas. Each story also has the fields of
// the platform, as the requirements of the catalog.
export type Criterion = { id: string; escenario: string; dado: string; cuando: string; entonces: string }
export type Task = { id: string; titulo: string }

export type Story = {
  id: string
  titulo: string
  como: string
  quiero: string
  para: string
  requisitos: string[]
  criterios: Criterion[]
  tareas: Task[]
  origen?: string
  discrepancia?: boolean
  explicacion?: string
  revisada?: boolean
}

export type Epic = { id: string; titulo: string; descripcion: string; historias: Story[] }

export type BacklogContent = { epicas: Epic[] }

// Every message of an ASP.NET Core "problem details" response (validation can
// give many), or the HTTP status.
export async function errorMessages(response: Response): Promise<string[]> {
  try {
    const problem: { errors?: Record<string, string[]> } = await response.json()
    const all = Object.values(problem.errors ?? {}).flat()
    if (all.length > 0) return all
  } catch {
    // The body was not JSON; use the status below.
  }
  return [`HTTP ${response.status}`]
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
