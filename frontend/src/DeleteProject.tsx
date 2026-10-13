import { useState, type FormEvent } from 'react'
import { Trash2 } from 'lucide-react'
import { errorMessage, type Project } from './api.ts'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'

// Deleting the project, at the end of its settings. It cannot be undone, so
// it asks the user to type the name of the project; the API checks the name
// again and refuses while a job is in progress.
function DeleteProject({ project, onDeleted }: { project: Project; onDeleted: () => void }) {
  const [asking, setAsking] = useState(false)
  const [typed, setTyped] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const confirmed = typed.trim() === project.name

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!confirmed) return
    setDeleting(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${project.id}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmation: typed }),
      })
      if (!response.ok) {
        setError(await errorMessage(response))
        return
      }
      onDeleted()
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setDeleting(false)
    }
  }

  function cancel() {
    setAsking(false)
    setTyped('')
    setError(null)
  }

  return (
    <section className="mt-8 space-y-3 border-t border-line pt-6">
      <h3 className="font-medium">Eliminar el proyecto</h3>
      <p className="text-sm text-muted">
        Se borran sus documentos, preguntas y respuestas, el catálogo, el backlog, el mockup y todas sus ejecuciones. No se puede deshacer.
      </p>

      {!asking ? (
        <Button variant="danger" icon={Trash2} onClick={() => setAsking(true)}>
          Eliminar el proyecto
        </Button>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-3 rounded border border-danger/40 bg-danger-soft/50 p-3">
          <label htmlFor="confirm-delete" className="block text-sm">
            Para confirmarlo, escribe el nombre del proyecto: <span className="font-semibold">{project.name}</span>
          </label>
          <input
            id="confirm-delete"
            autoFocus
            autoComplete="off"
            value={typed}
            onChange={(e) => setTyped(e.target.value)}
            className="w-full rounded border border-line bg-surface px-3 py-2 text-sm focus:outline-2 focus:outline-danger"
          />
          {error && <Banner tone="danger">{error}</Banner>}
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              disabled={!confirmed || deleting}
              className="inline-flex h-9 items-center gap-2 rounded bg-danger px-3.5 text-sm font-medium text-white hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40 dark:text-canvas"
            >
              <Trash2 className="size-4" aria-hidden />
              {deleting ? 'Eliminando…' : 'Eliminar definitivamente'}
            </button>
            <Button onClick={cancel}>Cancelar</Button>
          </div>
        </form>
      )}
    </section>
  )
}

export default DeleteProject
