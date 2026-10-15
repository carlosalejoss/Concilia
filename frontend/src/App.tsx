import { useCallback, useEffect, useState } from 'react'
import { FolderOpen } from 'lucide-react'
import { errorMessage, type Project } from './api.ts'
import Sidebar, { type HealthState } from './Sidebar.tsx'
import ProjectWorkspace from './ProjectWorkspace.tsx'
import EmptyState from './ui/EmptyState.tsx'
import Logo from './ui/Logo.tsx'
import { useHashRoute } from './hooks/useHashRoute.ts'
import { useTheme } from './hooks/useTheme.ts'
import { usePersistentState } from './hooks/usePersistentState.ts'

// The layout (milestone 14): the projects on the left and, on the right, the
// project open in the URL (#/proyectos/27/catalogo), one step at a time.
function App() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [projects, setProjects] = useState<Project[]>([])
  const [route, navigate] = useHashRoute()
  const [theme, toggleTheme] = useTheme()
  const [collapsed, setCollapsed] = usePersistentState('concilia.sidebar.collapsed', false)

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
        collapsed={collapsed}
        onToggleCollapsed={() => setCollapsed(!collapsed)}
        onHome={() => navigate({ kind: 'home' })}
      />
      {/* relative: the hidden labels for screen readers (sr-only) are
          positioned absolutely; without a positioned ancestor they hang from
          the page instead of from <main>, and stretch the page's own scroll
          bar down to where they sit in a long list. */}
      <main className="relative min-w-0 flex-1 overflow-y-auto">
        {project && route.kind !== 'home' ? (
          // key: another project starts with empty state.
          <ProjectWorkspace
            key={project.id}
            project={project}
            route={route}
            navigate={navigate}
            onProjectSaved={(saved) => setProjects((current) => current.map((p) => (p.id === saved.id ? saved : p)))}
            onProjectDeleted={() => {
              setProjects((current) => current.filter((p) => p.id !== project.id))
              navigate({ kind: 'home' })
            }}
          />
        ) : (
          // Home: the mark and the name, what the tool does, and what to do
          // next, centred in the page.
          <div className="flex min-h-full items-center justify-center px-6 py-16">
            <div className="w-full max-w-lg text-center">
              <Logo className="mx-auto size-16" />
              <h1 className="mt-5 text-4xl font-semibold tracking-tight">Concilia</h1>
              <p className="mx-auto mt-3 max-w-md text-muted">
                Requisitos y backlog a partir de la documentación de un sistema. Dos modelos trabajan cada fase por separado y un árbitro une
                lo que proponen.
              </p>
              <div className="mt-10">
                <EmptyState icon={FolderOpen} title={projects.length === 0 ? 'Crea tu primer proyecto' : 'Elige un proyecto'}>
                  {projects.length === 0
                    ? 'Pulsa «+» en la barra lateral, ponle un nombre y sube la documentación del sistema.'
                    : 'Ábrelo en la barra lateral para ver sus pasos: documentos, preguntas, catálogo, backlog y mockup.'}
                </EmptyState>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}

export default App
