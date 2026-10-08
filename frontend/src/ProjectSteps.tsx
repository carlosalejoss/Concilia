import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  errorMessage,
  formatDate,
  type FlowStep,
  type FlowView,
  type JobView,
  type PhaseResult,
  type Project,
  type Run,
  type StepState,
  type StepStatus,
} from './api.ts'
import ProjectFiles from './ProjectFiles.tsx'

const STEP_NAMES: Record<FlowStep, string> = {
  Documents: 'Documentos',
  Questions: 'Preguntas',
  Catalog: 'Catálogo',
  Backlog: 'Backlog',
  Mockup: 'Mockup',
}

const STATE_NAMES: Record<StepState, string> = {
  Blocked: 'Bloqueada',
  Available: 'Disponible',
  InProgress: 'En curso',
  Draft: 'Borrador',
  Validated: 'Validada',
}

const STATE_COLORS: Record<StepState, string> = {
  Blocked: 'bg-gray-200 text-gray-700',
  Available: 'bg-blue-100 text-blue-800',
  InProgress: 'bg-amber-100 text-amber-800',
  Draft: 'bg-indigo-100 text-indigo-800',
  Validated: 'bg-green-100 text-green-800',
}

// The name of a state, with the words that fit each step (documents and
// rounds have no "draft": they are there or not).
function stateName(step: StepStatus): string {
  if (step.step === 'Documents') return step.state === 'Draft' ? 'Con archivos' : 'Sin archivos'
  if (step.step === 'Questions' && step.state === 'Draft') return 'Con rondas'
  if (step.step === 'Mockup' && step.state === 'Draft') return 'Generado'
  return STATE_NAMES[step.state]
}

// The launch button of each step, and the steps whose job does not exist yet
// (they arrive in later milestones of the v0 iteration).
const LAUNCH: Partial<Record<FlowStep, { label: string; url: (projectId: number) => string | null; notYet?: string }>> = {
  Questions: { label: 'Lanzar una ronda de preguntas', url: (id) => `/projects/${id}/question-rounds` },
  Catalog: { label: 'Generar el catálogo', url: () => null, notYet: 'Llega en el hito 10.' },
  Backlog: { label: 'Generar el backlog', url: () => null, notYet: 'Llega en el hito 11.' },
  Mockup: { label: 'Generar el mockup', url: () => null, notYet: 'Llega en el hito 12.' },
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// The project page by steps: Documentos → Preguntas → Catálogo → Backlog →
// Mockup, with the state of each one (GET /projects/{id}/flow) and its button,
// disabled with the reason when the step cannot be launched yet.
function ProjectSteps({ project, onShowJob }: { project: Project; onShowJob: (jobId: string) => void }) {
  const [flow, setFlow] = useState<FlowView | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [launching, setLaunching] = useState<FlowStep | null>(null)
  // Changes when a job is launched, so the list of rounds reloads.
  const [version, setVersion] = useState(0)

  const reload = useCallback(() => {
    fetchJson<FlowView>(`/projects/${project.id}/flow`)
      .then((view) => {
        setFlow(view)
        setVersion((v) => v + 1)
      })
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    reload()
  }, [reload])

  // While some step is in progress, reload every 3 s to see it end.
  const inProgress = flow?.steps.some((s) => s.state === 'InProgress') ?? false
  useEffect(() => {
    if (!inProgress) return
    const timer = setInterval(reload, 3000)
    return () => clearInterval(timer)
  }, [inProgress, reload])

  async function launch(step: FlowStep, url: string) {
    setLaunching(step)
    setError(null)
    try {
      const response = await fetch(url, { method: 'POST' })
      if (!response.ok) {
        setError(await errorMessage(response))
        return
      }
      reload()
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setLaunching(null)
    }
  }

  if (!flow) {
    return <p className="mt-6 text-gray-500">{error ?? 'Cargando los pasos…'}</p>
  }

  const bodies: Partial<Record<FlowStep, ReactNode>> = {
    Documents: <ProjectFiles project={project} onChange={reload} />,
    Questions: <QuestionRounds project={project} version={version} onShowJob={onShowJob} />,
  }

  return (
    <section className="mt-6">
      {error && <p className="mb-4 text-red-700">{error}</p>}
      <ol className="space-y-4">
        {flow.steps.map((step, index) => {
          const launchInfo = LAUNCH[step.step]
          const url = launchInfo?.url(project.id) ?? null
          const disabled = !step.canLaunch || url === null || launching !== null
          const why = !step.canLaunch ? step.reason : url === null ? launchInfo?.notYet : null
          return (
            <li key={step.step} className="rounded border border-gray-200 p-4">
              <div className="flex flex-wrap items-center gap-3">
                <h3 className="text-lg font-semibold">
                  {index + 1}. {STEP_NAMES[step.step]}
                </h3>
                <span className={`rounded px-2 py-0.5 text-sm ${STATE_COLORS[step.state]}`}>{stateName(step)}</span>
                {step.outdated && (
                  <span className="rounded bg-orange-100 px-2 py-0.5 text-sm text-orange-800">
                    Desactualizado: ha cambiado algo anterior
                  </span>
                )}
                {step.detail && <span className="text-sm text-gray-500">{step.detail}</span>}
              </div>
              {launchInfo && (
                <div className="mt-3 flex flex-wrap items-center gap-3">
                  <button
                    type="button"
                    disabled={disabled}
                    onClick={() => url && launch(step.step, url)}
                    title={why ?? undefined}
                    className="rounded bg-blue-600 px-4 py-2 text-white disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {launching === step.step ? 'Lanzando…' : launchInfo.label}
                  </button>
                  {why && <span className="text-sm text-gray-600">{why}</span>}
                </div>
              )}
              {bodies[step.step]}
            </li>
          )
        })}
      </ol>
    </section>
  )
}

const SLOT_NAMES: Record<string, string> = { A: 'A', B: 'B', ARB: 'Árbitro' }

function isFinished(run: Run): boolean {
  return run.state !== 'Queued' && run.state !== 'Running'
}

// The question rounds of the project (jobs "questions"): the state of A, B and
// the arbiter and, when it ends, which output counts. The questions themselves
// are stored and answered from milestone 9 on.
function QuestionRounds({ project, version, onShowJob }: { project: Project; version: number; onShowJob: (jobId: string) => void }) {
  const [rounds, setRounds] = useState<JobView[]>([])

  useEffect(() => {
    fetchJson<JobView[]>(`/projects/${project.id}/jobs`)
      .then((jobs) => setRounds(jobs.filter((j) => j.job.kind === 'questions')))
      .catch(() => setRounds([]))
  }, [project.id, version])

  if (rounds.length === 0) return null

  return (
    <ul className="mt-3 space-y-2 text-sm">
      {rounds.map((view, index) => {
        // The state that counts in each slot is that of its last attempt.
        const last = (slot: string) => view.runs.filter((r) => r.slot === slot).sort((a, b) => b.attempt - a.attempt)[0]
        const finished = view.runs.every(isFinished)
        return (
          <li key={view.job.id} className="flex flex-wrap items-center gap-3">
            <span className="font-semibold">Ronda {rounds.length - index}</span>
            <span className="text-gray-500">{formatDate(view.job.createdAt)}</span>
            {['A', 'B', 'ARB'].map((slot) => (
              <span key={slot}>
                {SLOT_NAMES[slot]}: {last(slot)?.state ?? '—'}
              </span>
            ))}
            {finished && <RoundResult jobId={view.job.id} />}
            <button type="button" onClick={() => onShowJob(view.job.id)} className="text-blue-700 underline">
              Ver ejecución
            </button>
          </li>
        )
      })}
    </ul>
  )
}

function RoundResult({ jobId }: { jobId: string }) {
  const [result, setResult] = useState<PhaseResult | null>(null)

  useEffect(() => {
    fetchJson<PhaseResult>(`/jobs/${jobId}/result`)
      .then(setResult)
      .catch(() => setResult(null))
  }, [jobId])

  if (!result) return null
  if (result.fuente === null) return <span className="text-red-700">Falló: {result.mensaje}</span>
  const questions = (result.salida as { preguntas?: unknown[] } | null)?.preguntas?.length ?? 0
  return (
    <span className={result.verificacion_cruzada ? 'text-green-700' : 'text-amber-700'}>
      {questions} preguntas · {result.verificacion_cruzada ? 'unión del árbitro' : `solo ${result.fuente}, sin verificación cruzada`}
    </span>
  )
}

export default ProjectSteps
