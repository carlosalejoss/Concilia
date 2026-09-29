import { useState, type FormEvent } from 'react'
import { errorMessage, type Project } from './api.ts'

type ProjectSettingsProps = {
  project: Project
  onSaved: (project: Project) => void
}

// The inputs of the requirements prompt: system name (copied into every
// requirement, with its article), language of the catalog and glossary.
function ProjectSettings({ project, onSaved }: ProjectSettingsProps) {
  const [systemName, setSystemName] = useState(project.systemName)
  const [language, setLanguage] = useState(project.language)
  const [glossary, setGlossary] = useState(project.glossary ?? '')
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null)
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setMessage(null)
    try {
      const response = await fetch(`/projects/${project.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: project.name, systemName, language, glossary }),
      })
      if (!response.ok) {
        setMessage({ ok: false, text: await errorMessage(response) })
        return
      }
      onSaved(await response.json())
      setMessage({ ok: true, text: 'Guardado.' })
    } catch (e: unknown) {
      setMessage({ ok: false, text: String(e) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Configuración del catálogo</h2>
      <form onSubmit={handleSubmit} className="mt-4 grid gap-3 md:grid-cols-[12rem_1fr]">
        <label htmlFor="systemName" className="pt-2">
          Nombre del sistema
        </label>
        <input
          id="systemName"
          value={systemName}
          onChange={(e) => setSystemName(e.target.value)}
          placeholder='Con artículo: "el sistema ReservaSalas"'
          maxLength={200}
          className="rounded border border-gray-300 px-3 py-2"
        />

        <label htmlFor="language" className="pt-2">
          Idioma del catálogo
        </label>
        <select
          id="language"
          value={language}
          onChange={(e) => setLanguage(e.target.value as Project['language'])}
          className="w-40 rounded border border-gray-300 px-3 py-2"
        >
          <option value="es">Español (es)</option>
          <option value="en">Inglés (en)</option>
        </select>

        <label htmlFor="glossary" className="pt-2">
          Glosario (opcional)
        </label>
        <textarea
          id="glossary"
          value={glossary}
          onChange={(e) => setGlossary(e.target.value)}
          rows={3}
          className="rounded border border-gray-300 px-3 py-2"
        />

        <div className="md:col-start-2">
          <button type="submit" disabled={saving} className="rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50">
            Guardar
          </button>
          {message && <span className={`ml-3 ${message.ok ? 'text-green-700' : 'text-red-700'}`}>{message.text}</span>}
        </div>
      </form>
    </section>
  )
}

export default ProjectSettings
