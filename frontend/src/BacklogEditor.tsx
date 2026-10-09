import { useEffect, useState } from 'react'
import type { BacklogContent, CatalogContent, Criterion, Deliverable, Epic, Project, Story } from './api.ts'
import DeliverableBar from './DeliverableBar.tsx'
import ListInput from './ListInput.tsx'
import { useDeliverable } from './useDeliverable.ts'

const ORIGIN_NAMES: Record<string, string> = { A: 'Solo A', B: 'Solo B', ambas: 'A y B', usuario: 'Usuario' }
const field = 'w-full rounded border border-gray-300 p-1 text-sm disabled:border-transparent disabled:bg-transparent'
const smallButton = 'rounded border border-gray-400 px-2 py-0.5 text-xs'

function newStory(): Story {
  return {
    id: 'nueva',
    titulo: '',
    como: '',
    quiero: '',
    para: '',
    requisitos: [],
    criterios: [{ id: 'nuevo', escenario: '', dado: '', cuando: '', entonces: '' }],
    tareas: [],
    origen: 'usuario',
    discrepancia: false,
    explicacion: 'Añadida por el usuario.',
    revisada: false,
  }
}

// The backlog step (milestone 11): the working copy as an editable tree, Épica
// → Historia → Criterios (Gherkin) and Tareas, with the requirements each story
// covers, the coverage of the functional requirements of the validated catalog,
// the discrepancies of the arbiter and the buttons to save, validate and reopen.
function BacklogEditor({ project, version, onChange }: { project: Project; version: number; onChange: () => void }) {
  const state = useDeliverable<BacklogContent>(project.id, 'backlog', version, onChange)
  const [catalog, setCatalog] = useState<CatalogContent | null>(null)

  useEffect(() => {
    fetch(`/projects/${project.id}/deliverables/catalog`)
      .then(async (response) => (response.ok ? ((await response.json()) as Deliverable<CatalogContent>).content : null))
      .then(setCatalog)
      .catch(() => setCatalog(null))
  }, [project.id, version])

  const { deliverable, draft, edit } = state
  if (!deliverable || !draft) return null
  const readOnly = deliverable.status === 'Validated'
  const epics = draft.epicas

  function setEpic(index: number, epic: Epic) {
    edit({ epicas: epics.map((e, i) => (i === index ? epic : e)) })
  }

  // Coverage: functional (RF) and integration (RI) requirements of the catalog
  // and the stories that cover each one.
  const stories = epics.flatMap((e) => e.historias)
  const functional = (catalog?.requisitos ?? []).filter((r) => r.tipo === 'RF' || r.tipo === 'RI')
  const storiesOf = (id: string) => stories.filter((h) => h.requisitos.includes(id)).map((h) => h.id)
  const uncovered = functional.filter((r) => storiesOf(r.id).length === 0)
  const discrepancies = stories.filter((h) => h.discrepancia).length
  const reviewed = stories.filter((h) => h.discrepancia && h.revisada).length

  return (
    <div className="mt-4">
      <p className="text-sm text-gray-600">
        {epics.length} épicas · {stories.length} historias
        {discrepancies > 0 && ` · ${reviewed} de ${discrepancies} discrepancias revisadas`}
        {!readOnly && ' · Los ids se renumeran al guardar.'}
      </p>
      {functional.length > 0 && (
        <details className="mt-2 text-sm">
          <summary className={`cursor-pointer ${uncovered.length > 0 ? 'text-amber-800' : 'text-green-700'}`}>
            Cobertura: {functional.length - uncovered.length} de {functional.length} requisitos funcionales con historia
          </summary>
          <ul className="mt-1 space-y-1">
            {functional.map((r) => {
              const covering = storiesOf(r.id)
              return (
                <li key={r.id} className={covering.length === 0 ? 'text-amber-800' : ''}>
                  <span className="font-mono">{r.id}</span> {r.texto} → {covering.length > 0 ? covering.join(', ') : 'sin historia'}
                </li>
              )
            })}
          </ul>
        </details>
      )}
      <DeliverableBar state={state} name="Backlog" />

      <div className="mt-3 space-y-4">
        {epics.map((epic, index) => (
          <EpicCard
            key={index}
            epic={epic}
            readOnly={readOnly}
            onChange={(e) => setEpic(index, e)}
            onRemove={() => edit({ epicas: epics.filter((_, i) => i !== index) })}
          />
        ))}
      </div>
      {!readOnly && (
        <button
          type="button"
          onClick={() => edit({ epicas: [...epics, { id: 'nueva', titulo: '', descripcion: '', historias: [newStory()] }] })}
          className="mt-2 rounded border border-gray-400 px-3 py-1 text-sm"
        >
          Añadir épica
        </button>
      )}
    </div>
  )
}

function EpicCard({ epic, readOnly, onChange, onRemove }: { epic: Epic; readOnly: boolean; onChange: (e: Epic) => void; onRemove: () => void }) {
  function setStory(index: number, story: Story) {
    onChange({ ...epic, historias: epic.historias.map((h, i) => (i === index ? story : h)) })
  }

  return (
    <div className="rounded border border-gray-300 p-3">
      <div className="flex items-center gap-2">
        <span className="font-mono text-sm font-semibold">{epic.id}</span>
        <input disabled={readOnly} value={epic.titulo} placeholder="Título de la épica" onChange={(e) => onChange({ ...epic, titulo: e.target.value })} className={`${field} font-semibold`} />
        {!readOnly && (
          <button type="button" onClick={onRemove} className="text-sm text-red-700 underline">
            Borrar épica
          </button>
        )}
      </div>
      <textarea disabled={readOnly} value={epic.descripcion} rows={2} placeholder="Descripción" onChange={(e) => onChange({ ...epic, descripcion: e.target.value })} className={`${field} mt-1`} />

      <div className="mt-2 space-y-3 pl-4">
        {epic.historias.map((story, index) => (
          <StoryCard
            key={index}
            story={story}
            readOnly={readOnly}
            onChange={(s) => setStory(index, s)}
            onRemove={() => onChange({ ...epic, historias: epic.historias.filter((_, i) => i !== index) })}
          />
        ))}
        {!readOnly && (
          <button type="button" onClick={() => onChange({ ...epic, historias: [...epic.historias, newStory()] })} className={smallButton}>
            Añadir historia
          </button>
        )}
      </div>
    </div>
  )
}

function StoryCard({ story: h, readOnly, onChange, onRemove }: { story: Story; readOnly: boolean; onChange: (s: Story) => void; onRemove: () => void }) {
  const pending = h.discrepancia && !h.revisada

  function setCriterion(index: number, change: Partial<Criterion>) {
    onChange({ ...h, criterios: h.criterios.map((c, i) => (i === index ? { ...c, ...change } : c)) })
  }

  return (
    <div className={`rounded border p-2 ${h.discrepancia ? (pending ? 'border-amber-300 bg-amber-50' : 'border-green-300 bg-green-50') : 'border-gray-200'}`}>
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-mono text-sm font-semibold">{h.id}</span>
        <span className="rounded bg-blue-50 px-2 py-0.5 text-xs text-blue-800">{ORIGIN_NAMES[h.origen ?? ''] ?? h.origen ?? '—'}</span>
        <input disabled={readOnly} value={h.titulo} placeholder="Título de la historia" onChange={(e) => onChange({ ...h, titulo: e.target.value })} className={`${field} flex-1`} />
        {!readOnly && (
          <button type="button" onClick={onRemove} className="text-sm text-red-700 underline">
            Borrar
          </button>
        )}
      </div>
      <div className="mt-1 grid grid-cols-1 gap-1 text-sm md:grid-cols-3">
        <label>
          Como
          <input disabled={readOnly} value={h.como} onChange={(e) => onChange({ ...h, como: e.target.value })} className={field} />
        </label>
        <label>
          Quiero
          <input disabled={readOnly} value={h.quiero} onChange={(e) => onChange({ ...h, quiero: e.target.value })} className={field} />
        </label>
        <label>
          Para
          <input disabled={readOnly} value={h.para} onChange={(e) => onChange({ ...h, para: e.target.value })} className={field} />
        </label>
      </div>
      <label className="mt-1 block text-sm">
        Requisitos
        <ListInput disabled={readOnly} values={h.requisitos} placeholder="RF-001, RNF-001" onChange={(requisitos) => onChange({ ...h, requisitos })} className={field} />
      </label>
      {h.discrepancia && (
        <p className="mt-1 text-sm">
          <span className="font-semibold">Discrepancia:</span> {h.explicacion}
          <label className="ml-3 inline-flex items-center gap-1">
            <input type="checkbox" disabled={readOnly} checked={h.revisada ?? false} onChange={(e) => onChange({ ...h, revisada: e.target.checked })} />
            revisada
          </label>
        </p>
      )}

      <p className="mt-2 text-sm font-semibold">Criterios de aceptación</p>
      <div className="space-y-1">
        {h.criterios.map((c, index) => (
          <div key={index} className="grid grid-cols-1 gap-1 text-sm md:grid-cols-[5rem_1fr_1fr_1fr_1fr_auto]">
            <span className="font-mono">{c.id}</span>
            <input disabled={readOnly} value={c.escenario} placeholder="Escenario" onChange={(e) => setCriterion(index, { escenario: e.target.value })} className={field} />
            <input disabled={readOnly} value={c.dado} placeholder="Dado" onChange={(e) => setCriterion(index, { dado: e.target.value })} className={field} />
            <input disabled={readOnly} value={c.cuando} placeholder="Cuando" onChange={(e) => setCriterion(index, { cuando: e.target.value })} className={field} />
            <input disabled={readOnly} value={c.entonces} placeholder="Entonces" onChange={(e) => setCriterion(index, { entonces: e.target.value })} className={field} />
            {!readOnly && (
              <button type="button" onClick={() => onChange({ ...h, criterios: h.criterios.filter((_, i) => i !== index) })} className="text-red-700 underline">
                Borrar
              </button>
            )}
          </div>
        ))}
        {!readOnly && (
          <button
            type="button"
            onClick={() => onChange({ ...h, criterios: [...h.criterios, { id: 'nuevo', escenario: '', dado: '', cuando: '', entonces: '' }] })}
            className={smallButton}
          >
            Añadir criterio
          </button>
        )}
      </div>

      <p className="mt-2 text-sm font-semibold">Tareas</p>
      <div className="space-y-1">
        {h.tareas.map((t, index) => (
          <div key={index} className="flex items-center gap-2 text-sm">
            <span className="font-mono">{t.id}</span>
            <input
              disabled={readOnly}
              value={t.titulo}
              onChange={(e) => onChange({ ...h, tareas: h.tareas.map((x, i) => (i === index ? { ...x, titulo: e.target.value } : x)) })}
              className={field}
            />
            {!readOnly && (
              <button type="button" onClick={() => onChange({ ...h, tareas: h.tareas.filter((_, i) => i !== index) })} className="text-red-700 underline">
                Borrar
              </button>
            )}
          </div>
        ))}
        {!readOnly && (
          <button type="button" onClick={() => onChange({ ...h, tareas: [...h.tareas, { id: 'nueva', titulo: '' }] })} className={smallButton}>
            Añadir tarea
          </button>
        )}
      </div>
    </div>
  )
}

export default BacklogEditor
