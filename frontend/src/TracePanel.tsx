import { useEffect, useState, type ReactNode } from 'react'
import { ArrowLeft, ChevronRight } from 'lucide-react'
import type { TraceFragment, TraceQuestion, Traceability } from './api.ts'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import Drawer from './ui/Drawer.tsx'

// What the panel shows: a requirement (RF-001) or a story (HU-001).
export type TraceTarget = { kind: 'requirement' | 'story'; id: string }

// The traceability panel (step 99): from a requirement, its fragments of the
// documentation with their text, the questions it rests on and the stories
// that cover it; from a story, its requirements and, through them, their
// fragments and questions. A requirement or a story in the panel opens its
// own traceability, and "Volver" goes back. It shows the saved catalog and
// backlog, so the editors hide its button while there are unsaved changes.
// The editors mount it only while it is open, so each time it starts again
// from `start` and loads what was saved last.
function TracePanel({ projectId, start, onClose }: { projectId: number; start: TraceTarget; onClose: () => void }) {
  const [trace, setTrace] = useState<Traceability | null>(null)
  const [error, setError] = useState<string | null>(null)
  // The elements visited, the one shown last.
  const [path, setPath] = useState<TraceTarget[]>([start])

  useEffect(() => {
    fetch(`/projects/${projectId}/traceability`)
      .then(async (response) => {
        if (!response.ok) throw new Error(`HTTP ${response.status}`)
        setTrace((await response.json()) as Traceability)
      })
      .catch((e: unknown) => setError(String(e)))
  }, [projectId])

  const current = path.at(-1)!
  const go = (target: TraceTarget) => setPath([...path, target])
  const back = () => setPath(path.slice(0, -1))

  return (
    <Drawer
      open
      wide
      title={`Trazabilidad de ${current.id}`}
      subtitle="Del catálogo y el backlog guardados"
      onClose={onClose}
    >
      {path.length > 1 && (
        <Button variant="ghost" size="sm" icon={ArrowLeft} onClick={back} className="-ml-2.5 mb-3">
          Volver a {path.at(-2)!.id}
        </Button>
      )}
      {error && <p className="text-sm text-danger">No se pudo cargar la trazabilidad: {error}</p>}
      {!error && !trace && <p className="text-sm text-muted">Cargando…</p>}
      {trace && current.kind === 'requirement' && <RequirementTrace trace={trace} id={current.id} onGo={go} />}
      {trace && current.kind === 'story' && <StoryTrace trace={trace} id={current.id} onGo={go} />}
    </Drawer>
  )
}

function RequirementTrace({ trace, id, onGo }: { trace: Traceability; id: string; onGo: (t: TraceTarget) => void }) {
  const r = trace.requirements.find((x) => x.id === id)
  if (!r) return <p className="text-sm text-muted">{id} no está en el catálogo guardado.</p>
  const hasBacklog = trace.stories.length > 0

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <p className="font-serif">{r.text || 'Requisito sin texto'}</p>
        <Marks aiGuess={r.aiGuess} pendingDiscrepancy={r.pendingDiscrepancy} />
      </header>

      <Section title="Fragmentos de la documentación" count={r.sources.length} empty="No cita ningún fragmento.">
        {r.sources.map((f) => (
          <FragmentItem key={f} fragment={fragmentOf(trace, f)} />
        ))}
      </Section>

      <Section title="Preguntas en que se apoya" count={r.questions.length} empty="No se apoya en ninguna pregunta.">
        {r.questions.map((q) => (
          <QuestionItem key={q} id={q} question={trace.questions.find((x) => x.id === q)} />
        ))}
      </Section>

      <Section
        title="Historias que lo cubren"
        count={r.stories.length}
        empty={hasBacklog ? 'Ninguna historia del backlog lo cubre.' : 'Todavía no hay backlog.'}
      >
        {r.stories.map((h) => {
          const story = trace.stories.find((x) => x.id === h)!
          return (
            <LinkItem key={h} id={h} onClick={() => onGo({ kind: 'story', id: h })}>
              {story.title || 'Historia sin título'}
              <span className="block text-xs text-muted">
                {story.epicId} · {story.epicTitle}
              </span>
            </LinkItem>
          )
        })}
      </Section>
    </div>
  )
}

function StoryTrace({ trace, id, onGo }: { trace: Traceability; id: string; onGo: (t: TraceTarget) => void }) {
  const h = trace.stories.find((x) => x.id === id)
  if (!h) return <p className="text-sm text-muted">{id} no está en el backlog guardado.</p>
  const requirements = h.requirements.map((rid) => trace.requirements.find((x) => x.id === rid))
  // The fragments and questions of its requirements, once each, with the
  // requirements that cite them.
  const citing = (field: 'sources' | 'questions') => {
    const by = new Map<string, string[]>()
    for (const r of requirements) for (const ref of r?.[field] ?? []) by.set(ref, [...(by.get(ref) ?? []), r!.id])
    return [...by.entries()]
  }
  const fragments = citing('sources')
  const questions = citing('questions')

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <p className="text-xs text-muted">
          {h.epicId} · {h.epicTitle}
        </p>
        <p className="font-medium">{h.title || 'Historia sin título'}</p>
        <p className="font-serif">{h.narrative}</p>
        <Marks aiGuess={false} pendingDiscrepancy={h.pendingDiscrepancy} />
      </header>

      <Section title="Requisitos que cubre" count={h.requirements.length} empty="No cita ningún requisito.">
        {h.requirements.map((rid, i) => {
          const r = requirements[i]
          return r ? (
            <LinkItem key={rid} id={rid} onClick={() => onGo({ kind: 'requirement', id: rid })}>
              <span className="line-clamp-2 font-serif">{r.text || 'Requisito sin texto'}</span>
            </LinkItem>
          ) : (
            <li key={rid} className="py-2 text-sm">
              <span className="font-medium text-muted">{rid}</span> <span className="text-danger">no está en el catálogo guardado.</span>
            </li>
          )
        })}
      </Section>

      <Section title="Fragmentos de la documentación" count={fragments.length} empty="Sus requisitos no citan ningún fragmento.">
        {fragments.map(([f, by]) => (
          <FragmentItem key={f} fragment={fragmentOf(trace, f)} citedBy={by} />
        ))}
      </Section>

      <Section title="Preguntas en que se apoya" count={questions.length} empty="Sus requisitos no se apoyan en ninguna pregunta.">
        {questions.map(([q, by]) => (
          <QuestionItem key={q} id={q} question={trace.questions.find((x) => x.id === q)} citedBy={by} />
        ))}
      </Section>
    </div>
  )
}

// A cited fragment that is not in the trace has no text either.
const fragmentOf = (trace: Traceability, id: string): TraceFragment =>
  trace.fragments.find((f) => f.id === id) ?? { id, document: id.split('#')[0], section: '', text: null }

function Marks({ aiGuess, pendingDiscrepancy }: { aiGuess: boolean; pendingDiscrepancy: boolean }) {
  if (!aiGuess && !pendingDiscrepancy) return null
  return (
    <p className="flex flex-wrap gap-1">
      {pendingDiscrepancy && <Badge tone="danger">Discrepancia sin revisar</Badge>}
      {aiGuess && <Badge tone="warn">Suposición de la IA</Badge>}
    </p>
  )
}

function Section({ title, count, empty, children }: { title: string; count: number; empty: string; children: ReactNode }) {
  return (
    <section>
      <h3 className="mb-1 flex items-center gap-2 text-sm font-semibold">
        {title} <Badge>{count}</Badge>
      </h3>
      {count === 0 ? <p className="text-sm text-muted">{empty}</p> : <ul className="divide-y divide-line">{children}</ul>}
    </section>
  )
}

function CitedBy({ ids }: { ids?: string[] }) {
  return ids ? <span className="text-xs text-muted"> · citado por {ids.join(', ')}</span> : null
}

function FragmentItem({ fragment: f, citedBy }: { fragment: TraceFragment; citedBy?: string[] }) {
  return (
    <li className="space-y-1 py-2.5">
      <p className="text-xs">
        <span className="font-medium text-muted">{f.id}</span>
        <span className="text-muted">
          {' '}
          · {f.document}
          {f.section && ` · ${f.section}`}
        </span>
        <CitedBy ids={citedBy} />
      </p>
      {f.text !== null ? (
        <blockquote className="border-l-2 border-line pl-3 font-serif text-sm whitespace-pre-line">{f.text}</blockquote>
      ) : (
        <p className="text-sm text-danger">No está en la documentación de la que salió el catálogo.</p>
      )}
    </li>
  )
}

function QuestionItem({ id, question: q, citedBy }: { id: string; question?: TraceQuestion; citedBy?: string[] }) {
  return (
    <li className="space-y-1 py-2.5">
      <p className="text-xs">
        <span className="font-medium text-muted">{id}</span>
        <CitedBy ids={citedBy} />
      </p>
      {q ? (
        <>
          <p className="text-sm">{q.text}</p>
          <p className="text-sm text-muted">
            {q.answer ? (
              <>
                Respuesta: <span className="text-fg">{q.answer}</span>
                {q.aiGuess && (
                  <>
                    {' '}
                    <Badge tone="warn">Suposición de la IA</Badge>
                  </>
                )}
              </>
            ) : (
              'Sin respuesta.'
            )}
          </p>
        </>
      ) : (
        <p className="text-sm text-danger">No existe en el proyecto.</p>
      )}
    </li>
  )
}

// A requirement or story of the panel, which opens its own traceability.
function LinkItem({ id, onClick, children }: { id: string; onClick: () => void; children: ReactNode }) {
  return (
    <li>
      <button type="button" onClick={onClick} className="flex w-full items-start gap-3 rounded py-2 text-left hover:bg-sunken">
        <span className="w-16 shrink-0 pt-0.5 text-sm font-medium text-muted">{id}</span>
        <span className="min-w-0 flex-1 text-sm">{children}</span>
        <ChevronRight className="mt-0.5 size-4 shrink-0 text-faint" aria-hidden />
      </button>
    </li>
  )
}

export default TracePanel
