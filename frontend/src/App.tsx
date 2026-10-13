import { useCallback, useEffect, useState } from 'react'
import { FolderOpen } from 'lucide-react'
import { errorMessage, type Project } from './api.ts'
import Sidebar, { type HealthState } from './Sidebar.tsx'
import ProjectWorkspace from './ProjectWorkspace.tsx'
import EmptyState from './ui/EmptyState.tsx'
import { useHashRoute } from './hooks/useHashRoute.ts'
import { useTheme } from './hooks/useTheme.ts'

// The layout (milestone 14): the projects on the left and, on the right, the
// project open in the URL (#/proyectos/27/catalogo), one step at a time.
function App() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [projects, setProjects] = useState<Project[]>([])
  const [route, navigate] = useHashRoute()
  const [theme, toggleTheme] = useTheme()

  // Ask the API for its status and the projects once, when the page loads.
  useEffect(() => {
    fetch('/health')
      .then((response) => setHealth(response.ok ? { kind: 'ok' } : { kind: 'error', message: `HTTP ${response.status}` }))
      .catch((e: unknown) => setHealth({ kind: 'error', message: String(e) }))
    fetch('/projects')
      .then(async (response) => (response.ok ? setProjects(await response.json()) : null))
      .catch(() => setProjects([]))
  }, [])

  const createProject = useCallback(
    async (name: string): Promise<string | null> => {
      try {
        const response = await fetch('/projects', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name }),
        })
        if (!response.ok) return await errorMessage(response)
        // The list is newest first; the new project opens straight away.
        const created: Project = await response.json()
        setProjects((current) => [created, ...current])
        navigate({ kind: 'step', projectId: created.id, step: null })
        return null
      } catch (e: unknown) {
        return String(e)
      }
    },
    [navigate],
  )

  const projectId = route.kind === 'home' ? null : route.projectId
  const project = projects.find((p) => p.id === projectId) ?? null

  return (
    <div className="flex h-screen overflow-hidden bg-canvas font-sans text-fg antialiased">
      <Sidebar
        projects={projects}
        selectedId={projectId}
        onSelect={(id) => navigate({ kind: 'step', projectId: id, step: null })}
        onCreate={createProject}
        health={health}
        theme={theme}
        onToggleTheme={toggleTheme}
      />
      <main className="min-w-0 flex-1 overflow-y-auto">
        {project && route.kind !== 'home' ? (
          // key: another project starts with empty state.
          <ProjectWorkspace
            key={project.id}
            project={project}
            route={route}
            navigate={navigate}
            onProjectSaved={(saved) => setProjects((current) => current.map((p) => (p.id === saved.id ? saved : p)))}
          />
        ) : (
          <div className="mx-auto max-w-xl px-6 py-24">
            <EmptyState icon={FolderOpen} title={projects.length === 0 ? 'Crea tu primer proyecto' : 'Elige un proyecto'}>
              {projects.length === 0
                ? 'Pulsa «+» en la barra lateral, ponle un nombre y sube la documentación del sistema.'
                : 'Ábrelo en la barra lateral para ver sus pasos: documentos, preguntas, catálogo, backlog y mockup.'}
            </EmptyState>
          </div>
        )}
      </main>
    </div>
  )
}

export default App
