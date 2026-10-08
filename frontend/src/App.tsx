import { useEffect, useState } from 'react'
import type { Project } from './api.ts'
import Projects from './Projects.tsx'
import Jobs from './Jobs.tsx'
import ProjectSettings from './ProjectSettings.tsx'
import ProjectSteps from './ProjectSteps.tsx'

type Tab = 'steps' | 'runs'

type HealthState =
  | { kind: 'loading' }
  | { kind: 'ok'; status: string }
  | { kind: 'error'; message: string }

function App() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [selectedProject, setSelectedProject] = useState<Project | null>(null)
  const [tab, setTab] = useState<Tab>('steps')
  // The job to select when the "Ejecuciones" tab opens from "Ver ejecución".
  const [jobToShow, setJobToShow] = useState<string | null>(null)

  function selectProject(project: Project | null) {
    setSelectedProject(project)
    setTab('steps')
    setJobToShow(null)
  }

  function showJob(jobId: string) {
    setJobToShow(jobId)
    setTab('runs')
  }

  // Ask the API for its status once, when the page loads.
  useEffect(() => {
    fetch('/health')
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`)
        }
        const body: { status: string } = await response.json()
        setHealth({ kind: 'ok', status: body.status })
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : String(error)
        setHealth({ kind: 'error', message })
      })
  }, [])

  return (
    <main className="mx-auto max-w-6xl p-8 font-sans">
      <h1 className="text-2xl font-bold">ReqArbiter</h1>
      <p className="mt-4">
        Estado de la API:{' '}
        {health.kind === 'loading' && <span className="text-gray-500">comprobando…</span>}
        {health.kind === 'ok' && <span className="font-semibold text-green-700">{health.status}</span>}
        {health.kind === 'error' && (
          <span className="font-semibold text-red-700">sin conexión ({health.message})</span>
        )}
      </p>
      <Projects selectedId={selectedProject?.id ?? null} onSelect={selectProject} />
      {/* key: a different project creates new components with empty state. */}
      {selectedProject && (
        <ProjectSettings key={`settings-${selectedProject.id}`} project={selectedProject} onSaved={setSelectedProject} />
      )}
      {selectedProject && (
        <nav className="mt-8 flex gap-2 border-b border-gray-200">
          {(
            [
              ['steps', 'Pasos'],
              ['runs', 'Ejecuciones'],
            ] as const
          ).map(([id, label]) => (
            <button
              key={id}
              type="button"
              onClick={() => setTab(id)}
              className={`-mb-px border-b-2 px-4 py-2 ${tab === id ? 'border-blue-600 font-semibold text-blue-700' : 'border-transparent text-gray-600'}`}
            >
              {label}
            </button>
          ))}
        </nav>
      )}
      {selectedProject && tab === 'steps' && (
        <ProjectSteps key={`steps-${selectedProject.id}`} project={selectedProject} onShowJob={showJob} />
      )}
      {selectedProject && tab === 'runs' && (
        <Jobs key={`runs-${selectedProject.id}-${jobToShow}`} project={selectedProject} initialJobId={jobToShow} />
      )}
    </main>
  )
}

export default App
