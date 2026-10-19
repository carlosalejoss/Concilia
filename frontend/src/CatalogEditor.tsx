import { useState } from 'react'
import { ChevronRight, ListChecks, Plus, Search, Trash2 } from 'lucide-react'
import type { CatalogContent, CatalogRequirement, Project } from './api.ts'
import DeliverableBar from './DeliverableBar.tsx'
import ListInput from './ListInput.tsx'
import { useDeliverable } from './useDeliverable.ts'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import Collapsible from './ui/Collapsible.tsx'
import EmptyState from './ui/EmptyState.tsx'
import Provenance from './ui/Provenance.tsx'

type ReqType = CatalogRequirement['tipo']

const TYPES: { id: ReqType; name: string }[] = [
  { id: 'RF', name: 'Funcionales' },
  { id: 'RNF', name: 'No funcionales' },
  { id: 'RI', name: 'Integración' },
  { id: 'RA', name: 'Arquitectura' },
  { id: 'RT', name: 'Técnicos' },
]
const NOTATIONS = ['EARS', 'MASTeR'] as const
const OBLIGATIONS = ['shall', 'should', 'may'] as const
const ORIGINS = [
  ['all', 'Cualquier origen'],
  ['ambas', 'De los dos modelos'],
  ['A', 'Solo de A'],
  ['B', 'Solo de B'],
  ['usuario', 'Del usuario'],
] as const

// A discrepancy of the arbiter that the user has not reviewed yet.
const pendingDiscrepancy = (r: CatalogRequirement) => !!r.discrepancia && !r.revisada

// The catalog step: the working copy grouped by type in folding groups, with
// a search and filters (origin, pending discrepancies). Each requirement is a
// line that opens to edit it; a discrepancy of the arbiter is marked in red
// until the user reviews it. Saving replaces the content (the platform
// renumbers the ids by type); a validated catalog is read-only until reopened.
function CatalogEditor({ project, version, onChange }: { project: Project; version: number; onChange: () => void }) {
  const state = useDeliverable<CatalogContent>(project.id, 'catalog', version, onChange)
  const { deliverable, draft, edit } = state
  const [search, setSearch] = useState('')
  const [origin, setOrigin] = useState<(typeof ORIGINS)[number][0]>('all')
  const [onlyPending, setOnlyPending] = useState(false)
  const [onlyAssumed, setOnlyAssumed] = useState(false)
  // The requirements open, by their position in the list.
  const [open, setOpen] = useState<Set<number>>(new Set())

  if (!deliverable || !draft) {
    return (
      <EmptyState icon={ListChecks} title="Todavía no hay catálogo">
        Cuando no quede ninguna pregunta pendiente, genera el catálogo: dos modelos escriben los requisitos y el árbitro los une.
      </EmptyState>
    )
  }

  const readOnly = deliverable.status === 'Validated'
  const requirements = draft.requisitos
  const discrepancies = requirements.filter((r) => r.discrepancia).length
  const pending = requirements.filter(pendingDiscrepancy).length
  // Step 93: the requirements that rest on a guess of the AI accepted without a look.
  const assumedQuestions = new Set(deliverable.assumedQuestionIds ?? [])
  const isAssumed = (r: CatalogRequirement) => r.preguntas.some((p) => assumedQuestions.has(p))
  const assumed = requirements.filter(isAssumed).length

  function update(index: number, change: Partial<CatalogRequirement>) {
    edit({ requisitos: requirements.map((r, i) => (i === index ? { ...r, ...change } : r)) })
  }

  function remove(index: number) {
    edit({ requisitos: requirements.filter((_, i) => i !== index) })
    setOpen(new Set())
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
    setOpen(new Set([requirements.length])) // the new one opens to write it
  }

  function toggle(index: number) {
    const next = new Set(open)
    if (next.has(index)) next.delete(index)
    else next.add(index)
    setOpen(next)
  }

  const needle = search.trim().toLowerCase()
  const visible = (r: CatalogRequirement) =>
    (origin === 'all' || r.origen === origin) &&
    (!onlyPending || pendingDiscrepancy(r)) &&
    (!onlyAssumed || isAssumed(r)) &&
    (needle === '' || `${r.id} ${r.texto} ${r.fuentes.join(' ')} ${r.preguntas.join(' ')}`.toLowerCase().includes(needle))
  const indexed = requirements.map((r, index) => ({ r, index }))

  const summary =
    `${requirements.length} requisitos` +
    (discrepancies > 0 ? `, ${discrepancies - pending} de ${discrepancies} discrepancias revisadas` : '') +
    '. Los ids se renumeran por tipo al guardar.'

  return (
    <div className="space-y-4">
      <DeliverableBar state={state} name="Catálogo" summary={summary} />

      <div className="flex flex-wrap items-center gap-3">
        <label className="flex min-w-48 flex-1 items-center gap-2 rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus-within:outline-2 focus-within:outline-accent">
          <Search className="size-4 text-faint" aria-hidden />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por id, texto, fuente o pregunta"
            className="w-full bg-transparent placeholder:text-faint focus:outline-none"
          />
        </label>
        <select
          value={origin}
          onChange={(e) => setOrigin(e.target.value as typeof origin)}
          aria-label="Filtrar por origen"
          className="rounded border border-line bg-surface px-2.5 py-1.5 text-sm"
        >
          {ORIGINS.map(([id, label]) => (
            <option key={id} value={id}>
              {label}
            </option>
          ))}
        </select>
        {discrepancies > 0 && (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={onlyPending} onChange={(e) => setOnlyPending(e.target.checked)} className="accent-accent" />
            Solo discrepancias sin revisar <Badge tone={pending > 0 ? 'danger' : 'ok'}>{pending}</Badge>
          </label>
        )}
        {assumed > 0 && (
          <label className="flex items-center gap-2 text-sm" title="Requisitos que se apoyan en una respuesta que era una suposición de la IA y se aceptó tal cual">
            <input type="checkbox" checked={onlyAssumed} onChange={(e) => setOnlyAssumed(e.target.checked)} className="accent-accent" />
            Solo basados en suposiciones de la IA <Badge tone="warn">{assumed}</Badge>
          </label>
        )}
      </div>

      <div className="space-y-3">
        {TYPES.map(({ id, name }) => {
          const ofType = indexed.filter(({ r }) => r.tipo === id)
          if (ofType.length === 0) return null
          const shown = ofType.filter(({ r }) => visible(r))
          const pendingOfType = ofType.filter(({ r }) => pendingDiscrepancy(r)).length
          return (
            <Collapsible
              key={id}
              persistKey={`p${project.id}.catalogo.${id}`}
              defaultOpen={id === 'RF'}
              title={
                <>
                  {id} <span className="font-normal text-muted">{name}</span>
                </>
              }
              meta={
                <>
                  <Badge>{ofType.length}</Badge>
                  {pendingOfType > 0 && <Badge tone="danger">{pendingOfType} discrepancias</Badge>}
                </>
              }
            >
              {shown.length === 0 ? (
                <p className="text-sm text-muted">Ningún requisito de este tipo coincide con el filtro.</p>
              ) : (
                <ul className="divide-y divide-line">
                  {shown.map(({ r, index }) => (
                    <RequirementItem
                      key={index}
                      requirement={r}
                      readOnly={readOnly}
                      assumed={isAssumed(r)}
                      open={open.has(index)}
                      onToggle={() => toggle(index)}
                      onUpdate={(change) => update(index, change)}
                      onRemove={() => remove(index)}
                    />
                  ))}
                </ul>
              )}
            </Collapsible>
          )
        })}
      </div>

      {!readOnly && (
        <Button icon={Plus} onClick={add}>
          Añadir un requisito
        </Button>
      )}
    </div>
  )
}

// One requirement as a line (where it comes from, id, text and its labels)
// that opens to edit it or, in a validated catalog, to read its details.
function RequirementItem({
  requirement: r,
  readOnly,
  assumed,
  open,
  onToggle,
  onUpdate,
  onRemove,
}: {
  requirement: CatalogRequirement
  readOnly: boolean
  assumed: boolean
  open: boolean
  onToggle: () => void
  onUpdate: (change: Partial<CatalogRequirement>) => void
  onRemove: () => void
}) {
  const discrepancy = pendingDiscrepancy(r)
  const field = 'w-full rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus:outline-2 focus:outline-accent'
  const label = 'text-xs font-medium text-muted'

  return (
    <li className={discrepancy ? 'bg-danger-soft/40' : ''}>
      <button type="button" onClick={onToggle} aria-expanded={open} className="flex w-full items-start gap-3 py-2.5 text-left">
        <ChevronRight className={`mt-1 size-4 shrink-0 text-faint transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
        <Provenance origin={r.origen} discrepancy={discrepancy} />
        <span className="w-16 shrink-0 pt-0.5 text-sm font-medium text-muted">{r.id}</span>
        <span className={`min-w-0 flex-1 font-serif ${open ? '' : 'line-clamp-2'} ${r.texto === '' ? 'text-faint italic' : ''}`}>
          {r.texto || 'Requisito sin texto'}
        </span>
        <span className="flex shrink-0 flex-wrap justify-end gap-1">
          {r.notacion === 'MASTeR' && <Badge>MASTeR</Badge>}
          {r.obligatoriedad !== 'shall' && <Badge tone="info">{r.obligatoriedad}</Badge>}
          {r.discrepancia && <Badge tone={discrepancy ? 'danger' : 'ok'}>{discrepancy ? 'Discrepancia' : 'Revisada'}</Badge>}
          {assumed && (
            <span title="Se apoya en una respuesta que era una suposición de la IA y se aceptó tal cual: confírmala con el cliente">
              <Badge tone="warn">Suposición de la IA</Badge>
            </span>
          )}
        </span>
      </button>

      {open && (
        <div className="space-y-3 pb-4 pl-[5.75rem]">
          {r.discrepancia && (
            <div className={`rounded px-3 py-2 text-sm ${discrepancy ? 'bg-danger-soft' : 'bg-ok-soft'}`}>
              <p>
                <span className="font-medium">Discrepancia entre los modelos.</span> {r.explicacion}
              </p>
              <label className="mt-2 flex items-center gap-2">
                <input
                  type="checkbox"
                  disabled={readOnly}
                  checked={r.revisada ?? false}
                  onChange={(e) => onUpdate({ revisada: e.target.checked })}
                  className="accent-accent"
                />
                La he revisado
              </label>
            </div>
          )}

          {readOnly ? (
            <dl className="grid gap-x-4 gap-y-1 text-sm sm:grid-cols-[8rem_1fr]">
              <dt className="text-muted">Notación</dt>
              <dd>
                {r.notacion}, {r.obligatoriedad}
              </dd>
              <dt className="text-muted">Fuentes</dt>
              <dd className="text-muted">{r.fuentes.join(', ') || 'Ninguna'}</dd>
              <dt className="text-muted">Preguntas</dt>
              <dd className="text-muted">{r.preguntas.join(', ') || 'Ninguna'}</dd>
              {!r.discrepancia && r.explicacion && (
                <>
                  <dt className="text-muted">Nota del árbitro</dt>
                  <dd className="text-muted">{r.explicacion}</dd>
                </>
              )}
            </dl>
          ) : (
            <>
              <textarea
                value={r.texto}
                rows={3}
                onChange={(e) => onUpdate({ texto: e.target.value })}
                placeholder="El sistema DEBERÁ…"
                aria-label="Texto del requisito"
                className={`${field} font-serif`}
              />
              <div className="grid gap-3 sm:grid-cols-3">
                <label className="space-y-1">
                  <span className={label}>Tipo</span>
                  <select value={r.tipo} onChange={(e) => onUpdate({ tipo: e.target.value as ReqType })} className={field}>
                    {TYPES.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.id}, {t.name.toLowerCase()}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="space-y-1">
                  <span className={label}>Notación</span>
                  <select value={r.notacion} onChange={(e) => onUpdate({ notacion: e.target.value as CatalogRequirement['notacion'] })} className={field}>
                    {NOTATIONS.map((n) => (
                      <option key={n}>{n}</option>
                    ))}
                  </select>
                </label>
                <label className="space-y-1">
                  <span className={label}>Obligatoriedad</span>
                  <select
                    value={r.obligatoriedad}
                    onChange={(e) => onUpdate({ obligatoriedad: e.target.value as CatalogRequirement['obligatoriedad'] })}
                    className={field}
                  >
                    {OBLIGATIONS.map((o) => (
                      <option key={o}>{o}</option>
                    ))}
                  </select>
                </label>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <label className="space-y-1">
                  <span className={label}>Fuentes, separadas por comas</span>
                  <ListInput disabled={false} values={r.fuentes} onChange={(fuentes) => onUpdate({ fuentes })} className={field} placeholder="DOC-01#F002" />
                </label>
                <label className="space-y-1">
                  <span className={label}>Preguntas en que se apoya</span>
                  <ListInput disabled={false} values={r.preguntas} onChange={(preguntas) => onUpdate({ preguntas })} className={field} placeholder="P-003" />
                </label>
              </div>
              {!r.discrepancia && r.explicacion && <p className="text-sm text-muted">Nota del árbitro: {r.explicacion}</p>}
              <Button variant="danger" size="sm" icon={Trash2} onClick={onRemove}>
                Borrar el requisito
              </Button>
            </>
          )}
        </div>
      )}
    </li>
  )
}

export default CatalogEditor
