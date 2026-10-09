import type { CatalogContent, CatalogRequirement, Project } from './api.ts'
import DeliverableBar from './DeliverableBar.tsx'
import { useDeliverable } from './useDeliverable.ts'

const TYPES = ['RF', 'RNF', 'RA', 'RT', 'RI'] as const
const NOTATIONS = ['EARS', 'MASTeR'] as const
const OBLIGATIONS = ['shall', 'should', 'may'] as const
const ORIGIN_NAMES: Record<string, string> = { A: 'Solo A', B: 'Solo B', ambas: 'A y B', usuario: 'Usuario' }

// A list typed by the user as "DOC-01#F002, DOC-01#F004" -> ["DOC-01#F002", "DOC-01#F004"].
function splitList(text: string): string[] {
  return text
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

// The catalog step (milestone 10): the working copy as an editable table. The
// user edits text, type, notation, obligation and sources, adds and deletes
// requirements, and marks each discrepancy of the arbiter as reviewed. Saving
// replaces the content (the platform renumbers the ids by type); a validated
// catalog is read-only until it is reopened.
function CatalogEditor({ project, version, onChange }: { project: Project; version: number; onChange: () => void }) {
  const state = useDeliverable<CatalogContent>(project.id, 'catalog', version, onChange)
  const { deliverable, draft, edit } = state
  if (!deliverable || !draft) return null

  const readOnly = deliverable.status === 'Validated'
  const requirements = draft.requisitos
  const discrepancies = requirements.filter((r) => r.discrepancia).length
  const reviewed = requirements.filter((r) => r.discrepancia && r.revisada).length

  function update(index: number, change: Partial<CatalogRequirement>) {
    edit({ requisitos: requirements.map((r, i) => (i === index ? { ...r, ...change } : r)) })
  }

  function remove(index: number) {
    edit({ requisitos: requirements.filter((_, i) => i !== index) })
  }

  function add() {
    edit({
      requisitos: [
        ...requirements,
        {
          id: 'nuevo',
          tipo: 'RF',
          notacion: 'EARS',
          obligatoriedad: 'shall',
          texto: '',
          fuentes: [],
          preguntas: [],
          origen: 'usuario',
          discrepancia: false,
          explicacion: 'Añadido por el usuario.',
          revisada: false,
        },
      ],
    })
  }

  const field = 'w-full rounded border border-gray-300 p-1 text-sm disabled:border-transparent disabled:bg-transparent'

  return (
    <div className="mt-4">
      <p className="text-sm text-gray-600">
        {requirements.length} requisitos
        {discrepancies > 0 && ` · ${reviewed} de ${discrepancies} discrepancias revisadas`}
        {!readOnly && ' · Los ids se renumeran por tipo al guardar.'}
      </p>
      <DeliverableBar state={state} name="Catálogo" />

      <div className="mt-3 overflow-x-auto">
        <table className="w-full min-w-[60rem] text-left text-sm">
          <thead className="text-gray-500">
            <tr>
              <th className="p-1">Id</th>
              <th className="p-1">Tipo</th>
              <th className="p-1">Notación</th>
              <th className="p-1">Oblig.</th>
              <th className="w-2/5 p-1">Texto</th>
              <th className="p-1">Fuentes</th>
              <th className="p-1">Preguntas</th>
              <th className="p-1">Origen</th>
              {!readOnly && <th className="p-1" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {requirements.map((r, index) => (
              <RequirementRow
                key={index}
                requirement={r}
                readOnly={readOnly}
                field={field}
                onUpdate={(change) => update(index, change)}
                onRemove={() => remove(index)}
              />
            ))}
          </tbody>
        </table>
      </div>
      {!readOnly && (
        <button type="button" onClick={add} className="mt-2 rounded border border-gray-400 px-3 py-1 text-sm">
          Añadir requisito
        </button>
      )}
    </div>
  )
}

// One requirement; a discrepancy of the arbiter is shown below it, highlighted,
// with its explanation and the "revisada" mark.
function RequirementRow({
  requirement: r,
  readOnly,
  field,
  onUpdate,
  onRemove,
}: {
  requirement: CatalogRequirement
  readOnly: boolean
  field: string
  onUpdate: (change: Partial<CatalogRequirement>) => void
  onRemove: () => void
}) {
  const pending = r.discrepancia && !r.revisada
  return (
    <>
      <tr className={`align-top ${r.discrepancia ? (pending ? 'bg-amber-50' : 'bg-green-50') : ''}`}>
        <td className="p-1 font-mono">{r.id}</td>
        <td className="p-1">
          <select disabled={readOnly} value={r.tipo} onChange={(e) => onUpdate({ tipo: e.target.value as CatalogRequirement['tipo'] })} className={field}>
            {TYPES.map((t) => (
              <option key={t}>{t}</option>
            ))}
          </select>
        </td>
        <td className="p-1">
          <select disabled={readOnly} value={r.notacion} onChange={(e) => onUpdate({ notacion: e.target.value as CatalogRequirement['notacion'] })} className={field}>
            {NOTATIONS.map((n) => (
              <option key={n}>{n}</option>
            ))}
          </select>
        </td>
        <td className="p-1">
          <select
            disabled={readOnly}
            value={r.obligatoriedad}
            onChange={(e) => onUpdate({ obligatoriedad: e.target.value as CatalogRequirement['obligatoriedad'] })}
            className={field}
          >
            {OBLIGATIONS.map((o) => (
              <option key={o}>{o}</option>
            ))}
          </select>
        </td>
        <td className="p-1">
          <textarea disabled={readOnly} value={r.texto} rows={3} onChange={(e) => onUpdate({ texto: e.target.value })} className={field} />
        </td>
        <td className="p-1">
          <ListInput disabled={readOnly} values={r.fuentes} onChange={(fuentes) => onUpdate({ fuentes })} className={field} />
        </td>
        <td className="p-1">
          <ListInput disabled={readOnly} values={r.preguntas} onChange={(preguntas) => onUpdate({ preguntas })} className={field} />
        </td>
        <td className="p-1 whitespace-nowrap">{ORIGIN_NAMES[r.origen ?? ''] ?? r.origen ?? '—'}</td>
        {!readOnly && (
          <td className="p-1">
            <button type="button" onClick={onRemove} className="text-red-700 underline">
              Borrar
            </button>
          </td>
        )}
      </tr>
      {r.discrepancia && (
        <tr className={pending ? 'bg-amber-50' : 'bg-green-50'}>
          <td colSpan={readOnly ? 8 : 9} className="p-2 text-sm">
            <span className="font-semibold">Discrepancia:</span> {r.explicacion}
            <label className="ml-3 inline-flex items-center gap-1">
              <input type="checkbox" disabled={readOnly} checked={r.revisada ?? false} onChange={(e) => onUpdate({ revisada: e.target.checked })} />
              revisada
            </label>
          </td>
        </tr>
      )}
    </>
  )
}

// A comma-separated list edited as text; it becomes a list when the field loses focus.
function ListInput({
  values,
  onChange,
  disabled,
  className,
}: {
  values: string[]
  onChange: (values: string[]) => void
  disabled: boolean
  className: string
}) {
  return (
    <input
      disabled={disabled}
      defaultValue={values.join(', ')}
      key={values.join(',')}
      onBlur={(e) => {
        const next = splitList(e.target.value)
        if (next.join(',') !== values.join(',')) onChange(next)
      }}
      className={className}
    />
  )
}

export default CatalogEditor
