import { useState, type FormEvent } from 'react'
import { FolderKanban, Moon, Plus, Sun } from 'lucide-react'
import { formatDate, type Project } from './api.ts'
import Button from './ui/Button.tsx'
import type { Theme } from './hooks/useTheme.ts'

export type HealthState = { kind: 'loading' } | { kind: 'ok' } | { kind: 'error'; message: string }

type SidebarProps = {
  projects: Project[]
  selectedId: number | null
  onSelect: (projectId: number) => void
  // Creates a project and returns an error message, or null.
  onCreate: (name: string) => Promise<string | null>
  health: HealthState
  theme: Theme
  onToggleTheme: () => void
}

// The left column: the projects (newest first), a form to create one and, at
// the bottom, the state of the API and the light or dark mode.
function Sidebar({ projects, selectedId, onSelect, onCreate, health, theme, onToggleTheme }: SidebarProps) {
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault() // Stop the browser from reloading the page.
    setSaving(true)
    const failure = await onCreate(name)
    setSaving(false)
    setError(failure)
    if (!failure) {
      setName('')
      setCreating(false)
    }
  }

  return (
    <aside className="flex h-full w-64 shrink-0 flex-col border-r border-line bg-sunken">
      <div className="flex items-center gap-2 px-4 py-4">
        <span className="grid size-7 place-items-center rounded-md bg-accent text-accent-fg">
          <FolderKanban className="size-4" aria-hidden />
        </span>
        <span className="font-semibold tracking-tight">ReqArbiter</span>
      </div>

      <div className="flex items-center justify-between px-4 pt-2 pb-1">
        <span className="text-xs font-semibold tracking-wide text-faint uppercase">Proyectos</span>
        <Button variant="ghost" size="sm" icon={Plus} onClick={() => setCreating(!creating)} aria-label="Nuevo proyecto" title="Nuevo proyecto" />
      </div>

      {creating && (
        <form onSubmit={handleSubmit} className="space-y-2 px-3 pb-2">
          <input
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Nombre del proyecto"
            maxLength={200}
            className="w-full rounded-md border border-line bg-surface px-2.5 py-1.5 text-sm placeholder:text-faint focus:outline-2 focus:outline-accent"
          />
          <div className="flex gap-2">
            <Button type="submit" variant="primary" size="sm" disabled={saving || name.trim() === ''}>
              Crear
            </Button>
            <Button size="sm" onClick={() => setCreating(false)}>
              Cancelar
            </Button>
          </div>
          {error && <p className="text-xs text-danger">{error}</p>}
        </form>
      )}

      <nav className="flex-1 space-y-0.5 overflow-y-auto px-2 pb-3">
        {projects.length === 0 && <p className="px-2 py-2 text-sm text-faint">Todavía no hay proyectos.</p>}
        {projects.map((project) => {
          const selected = project.id === selectedId
          return (
            <button
              key={project.id}
              type="button"
              onClick={() => onSelect(project.id)}
              aria-current={selected ? 'page' : undefined}
              className={`block w-full rounded-md px-2.5 py-1.5 text-left text-sm transition-colors ${
                selected ? 'bg-accent-soft font-medium text-accent' : 'text-fg hover:bg-surface'
              }`}
            >
              <span className="block truncate">{project.name}</span>
              <span className="block text-xs text-faint">{formatDate(project.createdAt)}</span>
            </button>
          )
        })}
      </nav>

      <div className="flex items-center justify-between border-t border-line px-4 py-3 text-xs text-muted">
        <span className="flex items-center gap-2" title={health.kind === 'error' ? health.message : undefined}>
          <span
            className={`size-2 rounded-full ${health.kind === 'ok' ? 'bg-ok' : health.kind === 'error' ? 'bg-danger' : 'bg-faint'}`}
            aria-hidden
          />
          {health.kind === 'ok' ? 'API conectada' : health.kind === 'error' ? 'API sin conexión' : 'Comprobando…'}
        </span>
        <Button
          variant="ghost"
          size="sm"
          icon={theme === 'dark' ? Sun : Moon}
          onClick={onToggleTheme}
          aria-label={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
          title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
        />
      </div>
    </aside>
  )
}

export default Sidebar
