import { useState, type FormEvent } from 'react'
import { errorMessage, type Project } from './api.ts'
import Button from './ui/Button.tsx'

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

  const field = 'w-full rounded-md border border-line bg-surface px-3 py-2 text-sm placeholder:text-faint focus:outline-2 focus:outline-accent'
  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <p className="text-sm text-muted">Lo que reciben los prompts de todas las fases.</p>
      <div className="space-y-1.5">
        <label htmlFor="systemName" className="text-sm font-medium">
          Nombre del sistema
        </label>
        <input
          id="systemName"
          value={systemName}
          onChange={(e) => setSystemName(e.target.value)}
          placeholder='Con artículo: "el sistema ReservaSalas"'
          maxLength={200}
          className={field}
        />
        <p className="text-xs text-faint">Es el sujeto de cada requisito, tal cual.</p>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="language" className="text-sm font-medium">
          Idioma
        </label>
        <select
          id="language"
          value={language}
          onChange={(e) => setLanguage(e.target.value as Project['language'])}
          className={field}
        >
          <option value="es">Español (es)</option>
          <option value="en">Inglés (en)</option>
        </select>
      </div>

      <div className="space-y-1.5">
        <label htmlFor="glossary" className="text-sm font-medium">
          Glosario <span className="font-normal text-faint">(opcional)</span>
        </label>
        <textarea id="glossary" value={glossary} onChange={(e) => setGlossary(e.target.value)} rows={5} className={field} />
      </div>

      <div className="flex items-center gap-3">
        <Button type="submit" variant="primary" disabled={saving}>
          Guardar
        </Button>
        {message && <span className={`text-sm ${message.ok ? 'text-ok' : 'text-danger'}`}>{message.text}</span>}
      </div>
    </form>
  )
}

export default ProjectSettings
