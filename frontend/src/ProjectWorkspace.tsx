import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { Activity, ArrowLeft, Loader2, Play, Settings } from 'lucide-react'
import { errorMessage, type FlowStep, type FlowView, type Project, type StepStatus } from './api.ts'
import type { Route } from './hooks/useHashRoute.ts'
import StepBar from './StepBar.tsx'
import { STEP_NAMES, stateName } from './steps.ts'
import ProjectFiles from './ProjectFiles.tsx'
import QuestionsPanel from './QuestionsPanel.tsx'
import CatalogEditor from './CatalogEditor.tsx'
import BacklogEditor from './BacklogEditor.tsx'
import MockupViewer from './MockupViewer.tsx'
import Jobs from './Jobs.tsx'
import StepActivity from './StepActivity.tsx'
import ProjectSettings from './ProjectSettings.tsx'
import DeleteProject from './DeleteProject.tsx'
import Badge from './ui/Badge.tsx'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import Drawer from './ui/Drawer.tsx'

// What each step does, and the button that launches its job.
type LaunchOptions = { mockupStyle: boolean }

const STEP_INFO: Record<FlowStep, { description: string; launch?: { label: string; url: (projectId: number, options: LaunchOptions) => string } }> = {
  Documents: {
    description: 'La documentación del sistema: documentos, hojas de cálculo, presentaciones, correos, transcripciones de reuniones… Los modelos la leen por partes.',
  },
  Questions: {
    description: 'Dos modelos preparan las dudas para el cliente y un árbitro las une. Contesta o descarta cada una antes del catálogo.',
    launch: { label: 'Lanzar una ronda de preguntas', url: (id) => `/projects/${id}/question-rounds` },
  },
  Catalog: {
    description: 'Los requisitos en EARS y MASTeR, a partir de la documentación y las respuestas. Revisa las discrepancias y valídalo.',
    launch: { label: 'Generar el catálogo', url: (id) => `/projects/${id}/catalog-jobs` },
  },
  Backlog: {
    description: 'Épicas, historias, criterios de aceptación y tareas a partir del catálogo validado.',
    launch: { label: 'Generar el backlog', url: (id) => `/projects/${id}/backlog-jobs` },
  },
  Mockup: {
    description: 'Un prototipo navegable del backlog validado, en un único .html con datos de prueba.',
    // Without the style reference when the user chooses a design from scratch.
    launch: { label: 'Generar el mockup', url: (id, options) => `/projects/${id}/mockup-jobs${options.mockupStyle ? '' : '?estilo=false'}` },
  },
}

const STEP_TONE: Record<StepStatus['state'], 'neutral' | 'accent' | 'info' | 'warn' | 'ok'> = {
  Blocked: 'neutral',
  Available: 'accent',
  InProgress: 'info',
  Draft: 'warn',
  Validated: 'ok',
}

// The step to open when the URL does not say: the first one still to do
// (available or in progress) or, if all are done, the last one. It is chosen
// once, when the project opens (see firstStep).
function currentStep(steps: StepStatus[]): FlowStep {
  return (steps.find((s) => s.state === 'Available' || s.state === 'InProgress') ?? steps[steps.length - 1]).step
}

type ProjectWorkspaceProps = {
  project: Project
  route: Extract<Route, { kind: 'step' | 'runs' }>
  navigate: (route: Route) => void
  onProjectSaved: (project: Project) => void
  onProjectDeleted: () => void
}

// One project: its header (name, work in progress, runs and settings), the
// bar of steps and the step open in the URL; or the technical view of the runs.
function ProjectWorkspace({ project, route, navigate, onProjectSaved, onProjectDeleted }: ProjectWorkspaceProps) {
  const [flow, setFlow] = useState<FlowView | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [launching, setLaunching] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  // Changes each time the flow reloads, so the content of the step reloads too.
  const [version, setVersion] = useState(0)
  // The step shown when the URL does not name one, fixed when the flow first
  // loads. It does not move by itself when a file is added or a phase ends
  // (decision of 14-10-2026): the user goes on to the next step when they want.
  const [firstStep, setFirstStep] = useState<FlowStep | null>(null)
  // Mockup: follow the style reference (.zip) or design from scratch.
  const [mockupStyle, setMockupStyle] = useState(true)

  const reload = useCallback(() => {
    fetch(`/projects/${project.id}/flow`)
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorMessage(response))
        const loaded: FlowView = await response.json()
        setFlow(loaded)
        setFirstStep((step) => step ?? currentStep(loaded.steps))
        setVersion((v) => v + 1)
      })
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    reload()
  }, [reload])

  // While some step is in progress, reload every 3 s to see it end.
  const running = flow?.steps.filter((s) => s.state === 'InProgress') ?? []
  useEffect(() => {
    if (running.length === 0) return
    const timer = setInterval(reload, 3000)
    return () => clearInterval(timer)
  }, [running.length, reload])

  const showJob = (jobId: string) => navigate({ kind: 'runs', projectId: project.id, jobId })
  const closeSettings = useCallback(() => setSettingsOpen(false), [])

  async function launch(url: string) {
    setLaunching(true)
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
      setLaunching(false)
    }
  }

  const header = (
    <header className="flex flex-wrap items-center gap-3">
      <div className="min-w-0 flex-1">
        <h1 className="truncate text-2xl font-semibold tracking-tight">{project.name}</h1>
        <p className="mt-0.5 flex items-center gap-2 text-sm text-muted">
          <span className="truncate">{project.systemName}</span>
          <Badge>{project.language === 'es' ? 'Español' : 'Inglés'}</Badge>
        </p>
      </div>
      {running.length > 0 && (
        <Badge tone="info" icon={Loader2}>
          En curso: {running.map((s) => STEP_NAMES[s.step]).join(', ')}
        </Badge>
      )}
      <Button
        variant={route.kind === 'runs' ? 'primary' : 'secondary'}
        size="sm"
        icon={Activity}
        onClick={() => navigate(route.kind === 'runs' ? { kind: 'step', projectId: project.id, step: null } : { kind: 'runs', projectId: project.id, jobId: null })}
      >
        Ejecuciones
      </Button>
      <Button size="sm" icon={Settings} onClick={() => setSettingsOpen(true)}>
        Ajustes
      </Button>
    </header>
  )

  let content: ReactNode
  if (route.kind === 'runs') {
    content = (
      <>
        <Button variant="ghost" size="sm" icon={ArrowLeft} onClick={() => navigate({ kind: 'step', projectId: project.id, step: null })}>
          Volver a los pasos
        </Button>
        <Jobs key={route.jobId ?? 'all'} project={project} initialJobId={route.jobId} />
      </>
    )
  } else if (!flow) {
    content = <p className="text-muted">{error ?? 'Cargando los pasos…'}</p>
  } else {
    const stepId = route.step ?? firstStep ?? currentStep(flow.steps)
    const step = flow.steps.find((s) => s.step === stepId)!
    const info = STEP_INFO[stepId]
    const bodies: Record<FlowStep, ReactNode> = {
      Documents: <ProjectFiles project={project} onChange={reload} />,
      Questions: <QuestionsPanel project={project} version={version} onChange={reload} onShowJob={showJob} />,
      Catalog: <CatalogEditor project={project} version={version} onChange={reload} />,
      Backlog: <BacklogEditor project={project} version={version} onChange={reload} />,
      Mockup: <MockupViewer project={project} version={version} onShowJob={showJob} useStyle={mockupStyle} onUseStyleChange={setMockupStyle} />,
    }
    content = (
      <>
        <StepBar steps={flow.steps} current={stepId} onSelect={(s) => navigate({ kind: 'step', projectId: project.id, step: s })} />

        <section className="space-y-4">
          <div className="flex flex-wrap items-start gap-3 border-b border-line pb-4">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-xl font-semibold">{STEP_NAMES[stepId]}</h2>
                <Badge tone={STEP_TONE[step.state]}>{stateName(step)}</Badge>
                {step.detail && <span className="text-sm text-muted">{step.detail}</span>}
              </div>
              <p className="mt-1 max-w-prose text-sm text-muted">{info.description}</p>
            </div>
            {info.launch && (
              <Button
                variant="primary"
                icon={launching ? Loader2 : Play}
                disabled={!step.canLaunch || launching}
                title={step.canLaunch ? undefined : (step.reason ?? undefined)}
                onClick={() => launch(info.launch!.url(project.id, { mockupStyle }))}
              >
                {info.launch.label}
              </Button>
            )}
          </div>

          {step.outdated && <Banner tone="stale">Desactualizado: ha cambiado algo de un paso anterior. Puedes generarlo de nuevo.</Banner>}
          {info.launch && !step.canLaunch && step.reason && step.state !== 'InProgress' && <Banner tone="info">{step.reason}</Banner>}
          {step.warning && <Banner tone="warn">{step.warning}</Banner>}
          {error && <Banner tone="danger">{error}</Banner>}

          <div>{bodies[stepId]}</div>

          <StepActivity project={project} step={stepId} version={version} onShowJob={showJob} />
        </section>
      </>
    )
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-8 py-7">
      {header}
      {content}
      <Drawer open={settingsOpen} title="Ajustes del proyecto" onClose={closeSettings}>
        <ProjectSettings project={project} onSaved={onProjectSaved} />
        <DeleteProject project={project} onDeleted={onProjectDeleted} />
      </Drawer>
    </div>
  )
}

export default ProjectWorkspace
