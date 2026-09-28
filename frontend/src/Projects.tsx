import { useEffect, useState, type FormEvent } from 'react'

type Project = {
  id: number
  name: string
  createdAt: string
}

// Reads the first validation message from an ASP.NET Core "problem details"
// response, or falls back to the HTTP status.
async function errorMessage(response: Response): Promise<string> {
  try {
    const problem: { errors?: Record<string, string[]> } = await response.json()
    const first = Object.values(problem.errors ?? {})[0]?.[0]
    if (first) return first
  } catch {
    // The body was not JSON; use the status below.
  }
  return `HTTP ${response.status}`
}

function Projects() {
  const [projects, setProjects] = useState<Project[]>([])
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  // Load the list once, when the component appears.
  useEffect(() => {
    fetch('/projects')
      .then(async (response) => {
        if (!response.ok) {
          setError(await errorMessage(response))
          return
        }
        setProjects(await response.json())
      })
      .catch((e: unknown) => setError(String(e)))
  }, [])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault() // Stop the browser from reloading the page.
    setSaving(true)
    setError(null)
    try {
      const response = await fetch('/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name }),
      })
      if (!response.ok) {
        setError(await errorMessage(response))
        return
      }
      // The API returns the new project; put it first, as the list is newest first.
      const created: Project = await response.json()
      setProjects((current) => [created, ...current])
      setName('')
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Proyectos</h2>

      <form onSubmit={handleSubmit} className="mt-4 flex gap-2">
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Nombre del proyecto"
          maxLength={200}
          className="flex-1 rounded border border-gray-300 px-3 py-2"
        />
        <button
          type="submit"
          disabled={saving}
          className="rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
        >
          Crear
        </button>
      </form>

      {error && <p className="mt-2 text-red-700">{error}</p>}

      {projects.length === 0 ? (
        <p className="mt-4 text-gray-500">Todavía no hay proyectos.</p>
      ) : (
        <ul className="mt-4 divide-y divide-gray-200 rounded border border-gray-200">
          {projects.map((project) => (
            <li key={project.id} className="flex justify-between px-3 py-2">
              <span>{project.name}</span>
              <span className="text-sm text-gray-500">
                {new Date(project.createdAt).toLocaleString('es-ES')}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export default Projects
