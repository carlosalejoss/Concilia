import { useEffect, useState } from 'react'
import type { Project } from './api.ts'
import ProjectFiles from './ProjectFiles.tsx'
import Projects from './Projects.tsx'

type HealthState =
  | { kind: 'loading' }
  | { kind: 'ok'; status: string }
  | { kind: 'error'; message: string }

function App() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [selectedProject, setSelectedProject] = useState<Project | null>(null)

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
    <main className="mx-auto max-w-3xl p-8 font-sans">
      <h1 className="text-2xl font-bold">ReqArbiter</h1>
      <p className="mt-4">
        Estado de la API:{' '}
        {health.kind === 'loading' && <span className="text-gray-500">comprobando…</span>}
        {health.kind === 'ok' && <span className="font-semibold text-green-700">{health.status}</span>}
        {health.kind === 'error' && (
          <span className="font-semibold text-red-700">sin conexión ({health.message})</span>
        )}
      </p>
      <Projects selectedId={selectedProject?.id ?? null} onSelect={setSelectedProject} />
      {/* key: a different project creates a new ProjectFiles with empty state. */}
      {selectedProject && <ProjectFiles key={selectedProject.id} project={selectedProject} />}
    </main>
  )
}

export default App
