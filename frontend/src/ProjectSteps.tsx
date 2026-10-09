import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { errorMessage, type FlowStep, type FlowView, type Project, type StepState, type StepStatus } from './api.ts'
import ProjectFiles from './ProjectFiles.tsx'
import QuestionsPanel from './QuestionsPanel.tsx'
import CatalogEditor from './CatalogEditor.tsx'
import BacklogEditor from './BacklogEditor.tsx'
import MockupViewer from './MockupViewer.tsx'

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

// The launch button of each step.
const LAUNCH: Partial<Record<FlowStep, { label: string; url: (projectId: number) => string }>> = {
  Questions: { label: 'Lanzar una ronda de preguntas', url: (id) => `/projects/${id}/question-rounds` },
  Catalog: { label: 'Generar el catálogo', url: (id) => `/projects/${id}/catalog-jobs` },
  Backlog: { label: 'Generar el backlog', url: (id) => `/projects/${id}/backlog-jobs` },
  Mockup: { label: 'Generar el mockup', url: (id) => `/projects/${id}/mockup-jobs` },
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
  // Changes each time the flow reloads, so the rounds and questions reload too.
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
    Questions: <QuestionsPanel project={project} version={version} onChange={reload} onShowJob={onShowJob} />,
    Catalog: <CatalogEditor project={project} version={version} onChange={reload} />,
    Backlog: <BacklogEditor project={project} version={version} onChange={reload} />,
    Mockup: <MockupViewer project={project} version={version} onShowJob={onShowJob} />,
  }

  return (
    <section className="mt-6">
      {error && <p className="mb-4 text-red-700">{error}</p>}
      <ol className="space-y-4">
        {flow.steps.map((step, index) => {
          const launchInfo = LAUNCH[step.step]
          const url = launchInfo?.url(project.id) ?? null
          const disabled = !step.canLaunch || url === null || launching !== null
          const why = !step.canLaunch ? step.reason : null
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

export default ProjectSteps
