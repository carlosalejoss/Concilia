import { useState, type FormEvent } from 'react'
import { Moon, PanelLeftClose, PanelLeftOpen, Plus, Search, Sun, X } from 'lucide-react'
import { formatDay, searchable, type Project } from './api.ts'
import Logo from './ui/Logo.tsx'
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
  // Minimized: a narrow column with the logo and the buttons only.
  collapsed: boolean
  onToggleCollapsed: () => void
  // The logo leaves the project and goes to the home page.
  onHome: () => void
}

// The left column: the projects (newest first), a form to create one and, at
// the bottom, the state of the API and the light or dark mode. It can be
// minimized to a narrow column; its width changes with a transition, so the
// content of the page grows and centres smoothly.
function Sidebar({ projects, selectedId, onSelect, onCreate, health, theme, onToggleTheme, collapsed, onToggleCollapsed, onHome }: SidebarProps) {
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  // The search opens below the header with the magnifier; closing it clears it.
  const [searching, setSearching] = useState(false)
  const [query, setQuery] = useState('')

  function closeSearch() {
    setSearching(false)
    setQuery('')
  }

  const needle = searchable(query.trim())
  const shown = needle === '' ? projects : projects.filter((p) => searchable(p.name).includes(needle))

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

  const healthDot = (
    <span
      className={`size-2 shrink-0 rounded-full ${health.kind === 'ok' ? 'bg-ok' : health.kind === 'error' ? 'bg-danger' : 'bg-faint'}`}
      aria-hidden
    />
  )
  const healthText = health.kind === 'ok' ? 'API conectada' : health.kind === 'error' ? 'API sin conexión' : 'Comprobando…'
  const themeButton = (
    <Button
      variant="ghost"
      size="sm"
      icon={theme === 'dark' ? Sun : Moon}
      onClick={onToggleTheme}
      aria-label={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
      title={theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
    />
  )

  return (
    <aside
      className={`flex h-full shrink-0 flex-col overflow-hidden border-r border-line bg-sunken transition-[width] duration-300 ease-in-out ${
        collapsed ? 'w-14' : 'w-64'
      }`}
    >
      {/* The logo stays in the same place in both widths. */}
      <div className="flex items-center gap-2 px-4 py-4">
        <button
          type="button"
          onClick={onHome}
          title="Ir al inicio"
          aria-label="ReqArbiter: ir al inicio"
          className="flex min-w-0 items-center gap-2 rounded focus-visible:outline-2 focus-visible:outline-accent"
        >
          <Logo className="size-6 shrink-0" />
          <span className={`font-semibold tracking-tight whitespace-nowrap transition-opacity duration-200 ${collapsed ? 'opacity-0' : ''}`}>ReqArbiter</span>
        </button>
        {!collapsed && (
          <Button
            variant="ghost"
            size="sm"
            icon={PanelLeftClose}
            onClick={onToggleCollapsed}
            aria-label="Minimizar la barra lateral"
            title="Minimizar la barra lateral"
            className="ml-auto"
          />
        )}
      </div>

      {collapsed ? (
        <div className="flex flex-1 flex-col items-center gap-1 pb-3">
          <Button variant="ghost" size="sm" icon={PanelLeftOpen} onClick={onToggleCollapsed} aria-label="Desplegar la barra lateral" title="Desplegar la barra lateral" />
          <Button
            variant="ghost"
            size="sm"
            icon={Search}
            onClick={() => {
              setSearching(true)
              onToggleCollapsed()
            }}
            aria-label="Buscar proyectos"
            title="Buscar proyectos"
          />
          <Button
            variant="ghost"
            size="sm"
            icon={Plus}
            onClick={() => {
              setCreating(true)
              onToggleCollapsed()
            }}
            aria-label="Nuevo proyecto"
            title="Nuevo proyecto"
          />
          <div className="mt-auto flex flex-col items-center gap-3">
            <span title={healthText} className="flex size-8 items-center justify-center">
              {healthDot}
            </span>
            {themeButton}
          </div>
        </div>
      ) : (
        // A fixed width inside, so nothing reflows while the column grows.
        <div className="flex min-h-0 w-64 flex-1 flex-col">

          <div className="flex items-center justify-between px-4 pt-2 pb-1">
            <span className="text-sm font-medium text-muted">Proyectos</span>
            <span className="flex">
              <Button
                variant="ghost"
                size="sm"
                icon={Search}
                onClick={() => (searching ? closeSearch() : setSearching(true))}
                aria-label="Buscar proyectos"
                aria-expanded={searching}
                title="Buscar proyectos"
                className={searching ? 'bg-surface text-fg' : ''}
              />
              <Button variant="ghost" size="sm" icon={Plus} onClick={() => setCreating(!creating)} aria-label="Nuevo proyecto" title="Nuevo proyecto" />
            </span>
          </div>

          {searching && (
            <div className="px-3 pb-2">
              <label className="flex items-center gap-2 rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus-within:outline-2 focus-within:outline-accent">
                <Search className="size-4 shrink-0 text-faint" aria-hidden />
                <input
                  autoFocus
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  onKeyDown={(e) => e.key === 'Escape' && closeSearch()}
                  placeholder="Buscar por nombre"
                  aria-label="Buscar proyectos por nombre"
                  className="w-full min-w-0 bg-transparent placeholder:text-faint focus:outline-none"
                />
                {query !== '' && (
                  <button type="button" onClick={() => setQuery('')} aria-label="Borrar la búsqueda" className="text-faint hover:text-fg">
                    <X className="size-4" aria-hidden />
                  </button>
                )}
              </label>
            </div>
          )}

          {creating && (
            <form onSubmit={handleSubmit} className="space-y-2 px-3 pb-2">
              <input
                autoFocus
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Nombre del proyecto"
                maxLength={200}
                className="w-full rounded border border-line bg-surface px-2.5 py-1.5 text-sm placeholder:text-faint focus:outline-2 focus:outline-accent"
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
            {projects.length > 0 && shown.length === 0 && <p className="px-2 py-2 text-sm text-faint">Ningún proyecto se llama así.</p>}
            {shown.map((project) => {
              const selected = project.id === selectedId
              return (
                <button
                  key={project.id}
                  type="button"
                  onClick={() => onSelect(project.id)}
                  aria-current={selected ? 'page' : undefined}
                  className={`block w-full rounded px-2.5 py-1.5 text-left text-sm transition-colors ${
                    selected ? 'bg-accent-soft font-medium text-accent' : 'text-fg hover:bg-surface'
                  }`}
                >
                  <span className="block truncate">{project.name}</span>
                  <span className="block text-xs text-faint">{formatDay(project.createdAt)}</span>
                </button>
              )
            })}
          </nav>

          <div className="flex items-center justify-between border-t border-line px-4 py-3 text-xs text-muted">
            <span className="flex items-center gap-2" title={health.kind === 'error' ? health.message : undefined}>
              {healthDot}
              {healthText}
            </span>
            {themeButton}
          </div>
        </div>
      )}
    </aside>
  )
}

export default Sidebar
