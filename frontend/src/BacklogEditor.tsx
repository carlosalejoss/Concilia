import { useEffect, useState } from 'react'
import { ChevronRight, ChevronsDownUp, ChevronsUpDown, Layers, Plus, Search, Trash2, Waypoints } from 'lucide-react'
import { historyKey, type BacklogContent, type CatalogContent, type Criterion, type Deliverable, type Epic, type Project, type Story } from './api.ts'
import DeliverableBar from './DeliverableBar.tsx'
import VersionHistory from './VersionHistory.tsx'
import ListInput from './ListInput.tsx'
import TracePanel, { type TraceTarget } from './TracePanel.tsx'
import { useDeliverable } from './useDeliverable.ts'
import { usePersistentState } from './hooks/usePersistentState.ts'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import EmptyState from './ui/EmptyState.tsx'
import Provenance from './ui/Provenance.tsx'
import IterationBadge from './ui/IterationBadge.tsx'
import IterationFilter from './ui/IterationFilter.tsx'
import { iterationsOf, iterationsText, showIterations, storyIterations, useElementIterations } from './iterations.ts'
import { markOf, usePreviousVersion, type MarkedElement, type VersionMark } from './previousVersion.ts'
import { GoneElements, PreviousVersionBadge, PreviousVersionDetail, PreviousVersionFilter } from './PreviousVersionMarks.tsx'

const field = 'w-full rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus:outline-2 focus:outline-accent'
const label = 'text-xs font-medium text-muted'

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

const pendingDiscrepancy = (h: Story) => !!h.discrepancia && !h.revisada

// The backlog step: the working copy as a tree that folds, Épica → Historia →
// Criterios (Gherkin) and Tareas, with a search, a filter of unreviewed
// discrepancies, "open all" and "fold all", and the coverage of the functional
// requirements of the validated catalog. What is open is remembered. With
// more than one iteration, each story says those it comes from (those of its
// requirements), with a filter (step 111). Generated from scratch over an
// earlier backlog, each story says whether it is the same, changed or new,
// with a filter, and the ones that disappeared are listed apart (step 115).
function BacklogEditor({ project, version, onChange }: { project: Project; version: number; onChange: () => void }) {
  const state = useDeliverable<BacklogContent>(project.id, 'backlog', version, onChange)
  const [catalog, setCatalog] = useState<CatalogContent | null>(null)
  const [search, setSearch] = useState('')
  const [onlyPending, setOnlyPending] = useState(false)
  const [iteration, setIteration] = useState<number | null>(null)
  const iterationMap = useElementIterations(project.id, version)
  const previous = usePreviousVersion(state.deliverable?.jobId)
  const [mark, setMark] = useState<VersionMark | 'all'>('all')
  // Open epics ("0") and stories ("0.3"), by position.
  const [openKeys, setOpenKeys] = usePersistentState<string[]>(`concilia.open.p${project.id}.backlog`, ['0'])
  // Step 99: the story whose traceability is shown in the panel.
  const [trace, setTrace] = useState<TraceTarget | null>(null)

  useEffect(() => {
    fetch(`/projects/${project.id}/deliverables/catalog`)
      .then(async (response) => (response.ok ? ((await response.json()) as Deliverable<CatalogContent>).content : null))
      .then(setCatalog)
      .catch(() => setCatalog(null))
  }, [project.id, version])

  const { deliverable, draft, edit } = state
  if (!deliverable || !draft) {
    return (
      <EmptyState icon={Layers} title="Todavía no hay backlog">
        Con el catálogo validado, genera el backlog: épicas, historias, criterios de aceptación y tareas.
      </EmptyState>
    )
  }
  const readOnly = deliverable.status === 'Validated'
  const epics = draft.epicas
  const stories = epics.flatMap((e) => e.historias)

  const isOpen = (key: string) => openKeys.includes(key)
  const toggle = (key: string) => setOpenKeys(isOpen(key) ? openKeys.filter((k) => k !== key) : [...openKeys, key])
  const openAll = () => setOpenKeys(epics.flatMap((e, i) => [String(i), ...e.historias.map((_, j) => `${i}.${j}`)]))

  function setEpic(index: number, epic: Epic) {
    edit({ epicas: epics.map((e, i) => (i === index ? epic : e)) })
  }

  // Coverage: functional (RF) and integration (RI) requirements of the catalog
  // and the stories that cover each one.
  const functional = (catalog?.requisitos ?? []).filter((r) => r.tipo === 'RF' || r.tipo === 'RI')
  const storiesOf = (id: string) => stories.filter((h) => h.requisitos.includes(id)).map((h) => h.id)
  const uncovered = functional.filter((r) => storiesOf(r.id).length === 0)
  const discrepancies = stories.filter((h) => h.discrepancia).length
  const pending = stories.filter(pendingDiscrepancy).length
  // Step 111: the iterations of each requirement of the catalog, for those of each story.
  const requirementIterations = new Map(
    showIterations(iterationMap) ? (catalog?.requisitos ?? []).map((r) => [r.id, iterationsOf(iterationMap, r.fuentes, r.preguntas)] as const) : [],
  )
  const iterationsOfStory = (h: Story) => storyIterations(requirementIterations, h.requisitos)

  const needle = search.trim().toLowerCase()
  const visible = (h: Story) =>
    (!onlyPending || pendingDiscrepancy(h)) &&
    (iteration === null || iterationsOfStory(h).includes(iteration)) &&
    (mark === 'all' || markOf(previous, h.id)?.mark === mark) &&
    (needle === '' || `${h.id} ${h.titulo} ${h.como} ${h.quiero} ${h.para} ${h.requisitos.join(' ')}`.toLowerCase().includes(needle))
  const filtering = onlyPending || needle !== '' || iteration !== null || mark !== 'all'

  const changes = stories.filter((h) => h.cambio).length
  const summary =
    `${epics.length} épicas, ${stories.length} historias` +
    (discrepancies > 0 ? `, ${discrepancies - pending} de ${discrepancies} discrepancias revisadas` : '') +
    (changes > 0 ? `, ${changes} ${changes === 1 ? 'cambio propuesto' : 'cambios propuestos'} por revisar` : '') +
    '. Los ids no cambian al guardar: lo nuevo toma el siguiente número.'

  return (
    <div className="space-y-4">
      <DeliverableBar
        state={state}
        name="Backlog"
        summary={summary}
        downloads={[
          { href: `/projects/${project.id}/backlog-gherkin.zip`, label: 'Gherkin' },
          { href: `/projects/${project.id}/backlog-jira.csv`, label: 'CSV Jira' },
          { href: `/projects/${project.id}/backlog-azure-devops.csv`, label: 'CSV Azure DevOps' },
          { href: `/projects/${project.id}/backlog-github.zip`, label: 'GitHub' },
        ]}
      />

      <VersionHistory projectId={project.id} kind="backlog" refreshKey={historyKey(state.deliverable)} dirty={state.dirty} onRestored={onChange} />

      {functional.length > 0 && <Coverage functional={functional} uncovered={uncovered} storiesOf={storiesOf} projectId={project.id} />}

      <div className="flex flex-wrap items-center gap-3">
        <label className="flex min-w-48 flex-1 items-center gap-2 rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus-within:outline-2 focus-within:outline-accent">
          <Search className="size-4 text-faint" aria-hidden />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por id, título, rol, objetivo o requisito"
            className="w-full bg-transparent placeholder:text-faint focus:outline-none"
          />
        </label>
        {showIterations(iterationMap) && <IterationFilter count={iterationMap.count} value={iteration} onChange={setIteration} />}
        {previous && <PreviousVersionFilter comparison={previous} value={mark} onChange={setMark} />}
        {discrepancies > 0 && (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={onlyPending} onChange={(e) => setOnlyPending(e.target.checked)} className="accent-accent" />
            Solo discrepancias sin revisar <Badge tone={pending > 0 ? 'danger' : 'ok'}>{pending}</Badge>
          </label>
        )}
        <Button variant="ghost" size="sm" icon={ChevronsUpDown} onClick={openAll}>
          Desplegar todo
        </Button>
        <Button variant="ghost" size="sm" icon={ChevronsDownUp} onClick={() => setOpenKeys([])}>
          Plegar todo
        </Button>
      </div>

      <div className="space-y-3">
        {epics.map((epic, i) => {
          const shown = epic.historias.map((h, j) => ({ h, j })).filter(({ h }) => visible(h))
          if (filtering && shown.length === 0) return null
          const epicOpen = isOpen(String(i)) || filtering
          const pendingOfEpic = epic.historias.filter(pendingDiscrepancy).length
          return (
            <section key={i} className="rounded border border-line bg-surface">
              <button type="button" onClick={() => toggle(String(i))} aria-expanded={epicOpen} className="flex w-full items-center gap-2 px-3 py-2.5 text-left">
                <ChevronRight className={`size-4 shrink-0 text-faint transition-transform ${epicOpen ? 'rotate-90' : ''}`} aria-hidden />
                <span className="w-12 shrink-0 text-sm font-medium text-muted">{epic.id}</span>
                <span className={`min-w-0 flex-1 truncate font-medium ${epic.titulo === '' ? 'text-faint italic' : ''}`}>{epic.titulo || 'Épica sin título'}</span>
                <Badge>{epic.historias.length === 1 ? '1 historia' : `${epic.historias.length} historias`}</Badge>
                {pendingOfEpic > 0 && <Badge tone="danger">{pendingOfEpic} discrepancias</Badge>}
              </button>

              {epicOpen && (
                <div className="border-t border-line px-3 pb-3">
                  {readOnly ? (
                    epic.descripcion && <p className="max-w-prose py-2 pl-[5.25rem] text-sm text-muted">{epic.descripcion}</p>
                  ) : (
                    <div className="grid gap-2 py-3 pl-[5.25rem] sm:grid-cols-[1fr_auto]">
                      <input value={epic.titulo} placeholder="Título de la épica" aria-label="Título de la épica" onChange={(e) => setEpic(i, { ...epic, titulo: e.target.value })} className={`${field} font-medium`} />
                      <Button variant="danger" size="sm" icon={Trash2} onClick={() => edit({ epicas: epics.filter((_, k) => k !== i) })}>
                        Borrar la épica
                      </Button>
                      <textarea value={epic.descripcion} rows={2} placeholder="Descripción" aria-label="Descripción de la épica" onChange={(e) => setEpic(i, { ...epic, descripcion: e.target.value })} className={`${field} sm:col-span-2`} />
                    </div>
                  )}

                  <ul className="divide-y divide-line border-t border-line">
                    {shown.map(({ h, j }) => (
                      <StoryItem
                        key={j}
                        story={h}
                        iterations={iterationsOfStory(h)}
                        previous={markOf(previous, h.id)}
                        readOnly={readOnly}
                        // Step 114: on what is saved, so not while there are unsaved changes.
                        onDecide={state.dirty || state.busy ? undefined : (accept) => state.post(`/stories/${h.id}/${accept ? 'accept-change' : 'reject-change'}`)}
                        open={isOpen(`${i}.${j}`)}
                        // The panel shows what is saved: hidden while there are changes.
                        onTrace={state.dirty ? undefined : () => setTrace({ kind: 'story', id: h.id })}
                        onToggle={() => toggle(`${i}.${j}`)}
                        onChange={(s) => setEpic(i, { ...epic, historias: epic.historias.map((x, k) => (k === j ? s : x)) })}
                        onRemove={() => setEpic(i, { ...epic, historias: epic.historias.filter((_, k) => k !== j) })}
                      />
                    ))}
                  </ul>
                  {!readOnly && (
                    <Button
                      size="sm"
                      icon={Plus}
                      className="mt-3 ml-[5.25rem]"
                      onClick={() => {
                        setEpic(i, { ...epic, historias: [...epic.historias, newStory()] })
                        setOpenKeys([...openKeys, `${i}.${epic.historias.length}`])
                      }}
                    >
                      Añadir una historia
                    </Button>
                  )}
                </div>
              )}
            </section>
          )
        })}
        {previous && <GoneElements comparison={previous} persistKey={`p${project.id}.backlog.desaparecidas`} noun="Historias" />}
      </div>

      {!readOnly && (
        <Button
          icon={Plus}
          onClick={() => {
            edit({ epicas: [...epics, { id: 'nueva', titulo: '', descripcion: '', historias: [newStory()] }] })
            setOpenKeys([...openKeys, String(epics.length), `${epics.length}.0`])
          }}
        >
          Añadir una épica
        </Button>
      )}

      {trace && <TracePanel projectId={project.id} start={trace} onClose={() => setTrace(null)} />}
    </div>
  )
}

// How many functional requirements have a story: a bar and, folded, the list,
// the requirements without a story first.
function Coverage({
  functional,
  uncovered,
  storiesOf,
  projectId,
}: {
  functional: { id: string; texto: string }[]
  uncovered: { id: string; texto: string }[]
  storiesOf: (id: string) => string[]
  projectId: number
}) {
  const [open, setOpen] = usePersistentState(`concilia.open.p${projectId}.backlog.cobertura`, false)
  const covered = functional.length - uncovered.length
  const ordered = [...uncovered, ...functional.filter((r) => !uncovered.includes(r))]
  return (
    <div className="rounded border border-line bg-surface px-3 py-2.5">
      <button type="button" onClick={() => setOpen(!open)} aria-expanded={open} className="flex w-full items-center gap-3 text-left text-sm">
        <ChevronRight className={`size-4 shrink-0 text-faint transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
        <span className="font-medium">Cobertura</span>
        <span className="h-1.5 w-40 overflow-hidden rounded-full bg-sunken" aria-hidden>
          <span className={`block h-full ${uncovered.length > 0 ? 'bg-warn' : 'bg-ok'}`} style={{ width: `${(covered / functional.length) * 100}%` }} />
        </span>
        <span className={uncovered.length > 0 ? 'text-warn' : 'text-muted'}>
          {covered} de {functional.length} requisitos funcionales con historia
        </span>
      </button>
      {open && (
        <ul className="mt-2 max-h-72 space-y-1 overflow-y-auto pl-7 text-sm">
          {ordered.map((r) => {
            const covering = storiesOf(r.id)
            return (
              <li key={r.id} className="flex gap-3">
                <span className={`w-16 shrink-0 font-medium ${covering.length === 0 ? 'text-warn' : 'text-muted'}`}>{r.id}</span>
                <span className="line-clamp-1 min-w-0 flex-1 font-serif">{r.texto}</span>
                <span className={`shrink-0 ${covering.length === 0 ? 'text-warn' : 'text-muted'}`}>{covering.length > 0 ? covering.join(', ') : 'Sin historia'}</span>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

// One story as a line (where it comes from, id, title and its counts) that
// opens to show or edit the user story, its requirements, the discrepancy,
// the acceptance criteria in Gherkin and the tasks.
function StoryItem({
  story: h,
  iterations,
  previous,
  onDecide,
  readOnly,
  open,
  onTrace,
  onToggle,
  onChange,
  onRemove,
}: {
  story: Story
  iterations: number[]
  previous: MarkedElement | null
  onDecide?: (accept: boolean) => void
  readOnly: boolean
  open: boolean
  onTrace?: () => void
  onToggle: () => void
  onChange: (s: Story) => void
  onRemove: () => void
}) {
  const discrepancy = pendingDiscrepancy(h)

  function setCriterion(index: number, change: Partial<Criterion>) {
    onChange({ ...h, criterios: h.criterios.map((c, i) => (i === index ? { ...c, ...change } : c)) })
  }

  return (
    <li className={discrepancy ? 'bg-danger-soft/40' : ''}>
      <button type="button" onClick={onToggle} aria-expanded={open} className="flex w-full items-start gap-3 py-2.5 pl-6 text-left">
        <ChevronRight className={`mt-1 size-4 shrink-0 text-faint transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
        <Provenance origin={h.origen} discrepancy={discrepancy} />
        <span className="w-14 shrink-0 pt-0.5 text-sm font-medium text-muted">{h.id}</span>
        <span className="min-w-0 flex-1">
          <span className={`block ${h.titulo === '' ? 'text-faint italic' : ''}`}>{h.titulo || 'Historia sin título'}</span>
          {!open && h.quiero && <span className="line-clamp-1 font-serif text-sm text-muted">Como {h.como}, quiero {h.quiero}</span>}
        </span>
        <span className="flex shrink-0 flex-wrap justify-end gap-1">
          <IterationBadge numbers={iterations} />
          <PreviousVersionBadge element={previous} />
          {h.cambio && <Badge tone="danger">{h.cambio.accion === 'modificar' ? 'Modificación propuesta' : 'Eliminación propuesta'}</Badge>}
          <Badge>{h.criterios.length} criterios</Badge>
          {h.discrepancia && <Badge tone={discrepancy ? 'danger' : 'ok'}>{discrepancy ? 'Discrepancia' : 'Revisada'}</Badge>}
        </span>
      </button>

      {open && (
        <div className="space-y-4 pb-4 pl-[7.25rem]">
          {onTrace && (
            <Button variant="secondary" size="sm" icon={Waypoints} onClick={onTrace}>
              Trazabilidad
            </Button>
          )}
          <PreviousVersionDetail element={previous} />
          {/* Step 114: what an update proposes to modify or delete, reviewed before validating. */}
          {h.cambio && (
            <div className="space-y-2 rounded bg-danger-soft px-3 py-2 text-sm">
              <p>
                <span className="font-medium">
                  {h.cambio.accion === 'modificar' ? 'La actualización propone modificarla' : 'La actualización propone eliminarla'}
                </span>{' '}
                ({iterationsText(h.cambio.iteraciones)}): {h.cambio.motivo}
              </p>
              {h.cambio.accion === 'modificar' && h.cambio.antes && (
                <p className="text-xs">
                  Antes: Como {h.cambio.antes.como}, quiero {h.cambio.antes.quiero}, para {h.cambio.antes.para} ({h.cambio.antes.criterios?.length ?? 0} criterios y{' '}
                  {h.cambio.antes.tareas?.length ?? 0} tareas).
                </p>
              )}
              {h.cambio.discrepancia && <p className="text-xs">Los dos modelos no coincidían: {h.cambio.explicacion}</p>}
              <div className="flex flex-wrap items-center gap-2">
                <Button size="sm" variant="primary" disabled={readOnly || !onDecide} onClick={() => onDecide?.(true)}>
                  {h.cambio.accion === 'modificar' ? 'Aceptar la modificación' : 'Eliminarla'}
                </Button>
                <Button size="sm" disabled={readOnly || !onDecide} onClick={() => onDecide?.(false)}>
                  {h.cambio.accion === 'modificar' ? 'Rechazarla (vuelve como era)' : 'Mantenerla'}
                </Button>
                {!onDecide && !readOnly && <span className="text-xs">Guarda o deshaz tus cambios para revisarla.</span>}
              </div>
            </div>
          )}
          {(h.historial?.length ?? 0) > 0 && (
            <ul className="space-y-0.5 text-xs text-muted">
              {h.historial!.map((c, i) => (
                <li key={i}>
                  {c.accion === 'creada' ? 'Creada' : c.accion === 'modificada' ? 'Modificada' : 'Eliminada'} en {iterationsText(c.iteraciones)}: {c.motivo}
                </li>
              ))}
            </ul>
          )}
          {h.discrepancia && (
            <div className={`rounded px-3 py-2 text-sm ${discrepancy ? 'bg-danger-soft' : 'bg-ok-soft'}`}>
              <p>
                <span className="font-medium">Discrepancia entre los modelos.</span> {h.explicacion}
              </p>
              <label className="mt-2 flex items-center gap-2">
                <input type="checkbox" disabled={readOnly} checked={h.revisada ?? false} onChange={(e) => onChange({ ...h, revisada: e.target.checked })} className="accent-accent" />
                La he revisado
              </label>
            </div>
          )}

          {readOnly ? (
            <>
              <p className="max-w-prose font-serif">
                Como <span className="font-semibold">{h.como}</span>, quiero {h.quiero}, para {h.para}.
              </p>
              <p className="text-sm text-muted">Requisitos: {h.requisitos.join(', ') || 'ninguno'}</p>
            </>
          ) : (
            <>
              <div className="grid gap-2 sm:grid-cols-[1fr_auto]">
                <input value={h.titulo} placeholder="Título de la historia" aria-label="Título de la historia" onChange={(e) => onChange({ ...h, titulo: e.target.value })} className={field} />
                <Button variant="danger" size="sm" icon={Trash2} onClick={onRemove}>
                  Borrar la historia
                </Button>
              </div>
              <div className="grid gap-3 sm:grid-cols-3">
                {(['como', 'quiero', 'para'] as const).map((part) => (
                  <label key={part} className="space-y-1">
                    <span className={label}>{part[0].toUpperCase() + part.slice(1)}</span>
                    <input value={h[part]} onChange={(e) => onChange({ ...h, [part]: e.target.value })} className={`${field} font-serif`} />
                  </label>
                ))}
              </div>
              <label className="block space-y-1">
                <span className={label}>Requisitos que cubre, separados por comas</span>
                <ListInput disabled={false} values={h.requisitos} placeholder="RF-001, RNF-001" onChange={(requisitos) => onChange({ ...h, requisitos })} className={field} />
              </label>
            </>
          )}

          <div>
            <p className="text-sm font-medium">Criterios de aceptación</p>
            <ol className="mt-2 space-y-2">
              {h.criterios.map((c, index) => (
                <li key={index} className="rounded border border-line px-3 py-2">
                  {readOnly ? (
                    <>
                      <p className="text-sm">
                        <span className="text-muted">{c.id}</span> <span className="font-medium">{c.escenario}</span>
                      </p>
                      <dl className="mt-1 grid grid-cols-[5.5rem_1fr] gap-x-2 font-serif text-sm">
                        <dt className="font-sans text-muted">Dado</dt>
                        <dd>{c.dado}</dd>
                        <dt className="font-sans text-muted">Cuando</dt>
                        <dd>{c.cuando}</dd>
                        <dt className="font-sans text-muted">Entonces</dt>
                        <dd>{c.entonces}</dd>
                      </dl>
                    </>
                  ) : (
                    <div className="grid gap-2 sm:grid-cols-[1fr_auto]">
                      <input value={c.escenario} placeholder={`${c.id}: escenario`} aria-label="Escenario" onChange={(e) => setCriterion(index, { escenario: e.target.value })} className={`${field} font-medium`} />
                      <Button variant="ghost" size="sm" icon={Trash2} aria-label="Borrar el criterio" title="Borrar el criterio" onClick={() => onChange({ ...h, criterios: h.criterios.filter((_, i) => i !== index) })} />
                      {(['dado', 'cuando', 'entonces'] as const).map((part) => (
                        <input
                          key={part}
                          value={c[part]}
                          placeholder={part[0].toUpperCase() + part.slice(1)}
                          aria-label={part}
                          onChange={(e) => setCriterion(index, { [part]: e.target.value })}
                          className={`${field} font-serif sm:col-span-2`}
                        />
                      ))}
                    </div>
                  )}
                </li>
              ))}
            </ol>
            {!readOnly && (
              <Button variant="ghost" size="sm" icon={Plus} className="mt-2" onClick={() => onChange({ ...h, criterios: [...h.criterios, { id: 'nuevo', escenario: '', dado: '', cuando: '', entonces: '' }] })}>
                Añadir un criterio
              </Button>
            )}
          </div>

          <div>
            <p className="text-sm font-medium">Tareas</p>
            <ul className="mt-2 space-y-1">
              {h.tareas.map((t, index) => (
                <li key={index} className="flex items-center gap-3 text-sm">
                  <span className="w-12 shrink-0 text-muted">{t.id}</span>
                  {readOnly ? (
                    <span>{t.titulo}</span>
                  ) : (
                    <>
                      <input
                        value={t.titulo}
                        aria-label="Tarea"
                        onChange={(e) => onChange({ ...h, tareas: h.tareas.map((x, i) => (i === index ? { ...x, titulo: e.target.value } : x)) })}
                        className={field}
                      />
                      <Button variant="ghost" size="sm" icon={Trash2} aria-label="Borrar la tarea" title="Borrar la tarea" onClick={() => onChange({ ...h, tareas: h.tareas.filter((_, i) => i !== index) })} />
                    </>
                  )}
                </li>
              ))}
              {h.tareas.length === 0 && readOnly && <li className="text-sm text-muted">Sin tareas.</li>}
            </ul>
            {!readOnly && (
              <Button variant="ghost" size="sm" icon={Plus} className="mt-2" onClick={() => onChange({ ...h, tareas: [...h.tareas, { id: 'nueva', titulo: '' }] })}>
                Añadir una tarea
              </Button>
            )}
          </div>
        </div>
      )}
    </li>
  )
}

export default BacklogEditor
