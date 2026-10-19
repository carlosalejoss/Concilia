import { useCallback, useEffect, useState } from 'react'
import { CheckCheck, CheckCircle2, ChevronRight, CircleDot, MessageSquareText, MinusCircle, Search, Sparkles, Terminal, type LucideIcon } from 'lucide-react'
import {
  errorMessage,
  formatDay,
  SUGGESTIONS_KIND,
  type JobView,
  type PhaseResult,
  type Project,
  type Question,
  type QuestionStatus,
  type Run,
} from './api.ts'
import Badge from './ui/Badge.tsx'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import LaunchButton from './ui/LaunchButton.tsx'
import { useActiveJob } from './hooks/useActiveJob.ts'
import Collapsible from './ui/Collapsible.tsx'
import EmptyState from './ui/EmptyState.tsx'
import Provenance from './ui/Provenance.tsx'
import type { Tone } from './ui/tones.ts'

const ANSWER_ORIGIN_NAMES: Record<string, string> = {
  usuario: 'Escrita por el usuario',
  ia: 'Sugerencia de la IA aceptada',
  'ia-editada': 'Sugerencia de la IA editada',
}

const BASIS: Record<string, { label: string; tone: Tone }> = {
  documentacion: { label: 'Basada en la documentación', tone: 'ok' },
  suposicion: { label: 'Suposición: confírmala con el cliente', tone: 'warn' },
}

// The status of a question as a small icon on its line (a label on every
// line weighed too much), with its name for screen readers and on hover.
const STATUS: Record<QuestionStatus, { label: string; icon: LucideIcon; color: string }> = {
  Pending: { label: 'Pendiente', icon: CircleDot, color: 'text-warn' },
  Answered: { label: 'Respondida', icon: CheckCircle2, color: 'text-ok' },
  Discarded: { label: 'Descartada', icon: MinusCircle, color: 'text-faint' },
}

type Filter = 'all' | QuestionStatus

function isFinished(run: Run): boolean {
  return run.state !== 'Queued' && run.state !== 'Running'
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// Sends one action on a question and returns an error message, or null.
async function send(url: string, method: string, body?: unknown): Promise<string | null> {
  const response = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  return response.ok ? null : errorMessage(response)
}

// The questions step: the rounds of the project (the newest open, the others
// folded), a filter by status and a search, and one line per question that
// opens to answer it, discard it or reopen it. The AI can suggest answers (to
// one question or to all the pending ones of a round): a suggestion fills the
// answer box, but nothing is saved until the user saves it, edited or not.
// version changes when the page reloads the flow; onChange tells the page that
// a question changed (the catalog depends on the pending ones).
function QuestionsPanel({
  project,
  version,
  onChange,
  onShowJob,
}: {
  project: Project
  version: number
  onChange: () => void
  onShowJob: (jobId: string) => void
}) {
  const [rounds, setRounds] = useState<JobView[]>([])
  const [suggestionJobs, setSuggestionJobs] = useState<JobView[]>([])
  const [questions, setQuestions] = useState<Question[]>([])
  const [filter, setFilter] = useState<Filter>('all')
  const [search, setSearch] = useState('')
  const [error, setError] = useState<string | null>(null)
  const activeJob = useActiveJob()

  const load = useCallback(() => {
    Promise.all([
      fetchJson<JobView[]>(`/projects/${project.id}/jobs`),
      fetchJson<Question[]>(`/projects/${project.id}/questions`),
    ])
      .then(([jobs, list]) => {
        setRounds(jobs.filter((j) => j.job.kind === 'questions'))
        setSuggestionJobs(jobs.filter((j) => j.job.kind === SUGGESTIONS_KIND))
        setQuestions(list)
      })
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    load()
  }, [load, version])

  // Suggestions are not a step of the flow: while one is being generated, this
  // panel reloads itself every 3 s to show them when they arrive.
  const suggesting = suggestionJobs.some((j) => !j.runs.every(isFinished))
  useEffect(() => {
    if (!suggesting) return
    const timer = setInterval(load, 3000)
    return () => clearInterval(timer)
  }, [suggesting, load])

  async function act(url: string, method: string, body?: unknown): Promise<boolean> {
    setError(null)
    const failure = await send(url, method, body)
    if (failure) {
      setError(failure)
      return false
    }
    load()
    onChange()
    return true
  }

  async function suggest(url: string) {
    setError(null)
    const failure = await send(url, 'POST')
    if (failure) setError(failure)
    activeJob.refresh() // the other buttons go grey at once
    load()
  }

  if (rounds.length === 0) {
    return (
      <EmptyState icon={MessageSquareText} title="Todavía no hay preguntas">
        Lanza una ronda: dos modelos leen la documentación, preparan las dudas para el cliente y el árbitro las une.
      </EmptyState>
    )
  }

  const count = (status: QuestionStatus) => questions.filter((q) => q.status === status).length
  const needle = search.trim().toLowerCase()
  const visible = (q: Question) =>
    (filter === 'all' || q.status === filter) &&
    (needle === '' || `${q.code} ${q.text} ${q.reason} ${q.answer ?? ''}`.toLowerCase().includes(needle))
  // The jobs come newest first: if the last suggestions failed, it is told.
  const lastSuggestions = suggestionJobs[0]
  const lastFailed =
    lastSuggestions !== undefined && lastSuggestions.runs.every(isFinished) && lastSuggestions.runs.some((r) => r.state !== 'Done')

  const filters: [Filter, string, number][] = [
    ['all', 'Todas', questions.length],
    ['Pending', 'Pendientes', count('Pending')],
    ['Answered', 'Respondidas', count('Answered')],
    ['Discarded', 'Descartadas', count('Discarded')],
  ]

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div role="radiogroup" aria-label="Filtrar por estado" className="flex rounded border border-line bg-surface p-0.5">
          {filters.map(([id, label, n]) => (
            <button
              key={id}
              type="button"
              role="radio"
              aria-checked={filter === id}
              onClick={() => setFilter(id)}
              className={`rounded-sm px-2.5 py-1 text-sm transition-colors ${filter === id ? 'bg-accent-soft font-medium text-accent' : 'text-muted hover:text-fg'}`}
            >
              {label} <span className="text-faint">{n}</span>
            </button>
          ))}
        </div>
        <label className="flex min-w-48 flex-1 items-center gap-2 rounded border border-line bg-surface px-2.5 py-1.5 text-sm focus-within:outline-2 focus-within:outline-accent">
          <Search className="size-4 text-faint" aria-hidden />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar en las preguntas y respuestas"
            className="w-full bg-transparent placeholder:text-faint focus:outline-none"
          />
        </label>
      </div>

      {count('Pending') > 0 && (
        <Banner tone="warn">
          {count('Pending') === 1 ? 'Queda 1 pregunta pendiente' : `Quedan ${count('Pending')} preguntas pendientes`}: contéstalas o
          descártalas para poder generar el catálogo.
        </Banner>
      )}
      {suggesting && <Banner tone="info">Generando sugerencias de respuesta con IA…</Banner>}
      {!suggesting && lastFailed && (
        <Banner
          tone="danger"
          action={
            <Button variant="ghost" size="sm" onClick={() => onShowJob(lastSuggestions.job.id)}>
              Ver ejecución
            </Button>
          }
        >
          La última petición de sugerencias no terminó bien. Puedes pedirlas de nuevo.
        </Banner>
      )}
      {error && <Banner tone="danger">{error}</Banner>}

      <div className="space-y-3">
        {rounds.map((view, index) => {
          const ofRound = questions.filter((q) => q.jobId === view.job.id)
          const shown = ofRound.filter(visible)
          const pendingOfRound = ofRound.filter((q) => q.status === 'Pending').length
          // "Aprobar" only when the AI has suggested an answer for every
          // pending question of the round, and no suggestion is on its way.
          // Step 93: it approves only those that come from the documentation;
          // the guesses are reviewed one by one.
          const allSuggested = pendingOfRound > 0 && ofRound.every((q) => q.status !== 'Pending' || q.suggestedAnswer !== null)
          const documented = ofRound.filter((q) => q.status === 'Pending' && q.suggestionBasis === 'documentacion').length
          const finished = view.runs.every(isFinished)
          return (
            <Collapsible
              key={view.job.id}
              persistKey={`p${project.id}.ronda.${view.job.id}`}
              defaultOpen={index === 0}
              title={`Ronda ${rounds.length - index}`}
              meta={
                <>
                  <span className="text-sm text-muted">
                    {ofRound.length === 1 ? '1 pregunta' : `${ofRound.length} preguntas`}, {formatDay(view.job.createdAt)}
                  </span>
                  {pendingOfRound > 0 && <Badge tone="warn">{pendingOfRound} pendientes</Badge>}
                  {finished ? <RoundResult jobId={view.job.id} /> : <Badge tone="info">En curso</Badge>}
                </>
              }
              actions={
                <>
                  {allSuggested && documented > 0 && !suggesting && (
                    <Button
                      size="sm"
                      variant="primary"
                      icon={CheckCheck}
                      onClick={() => act(`/question-rounds/${view.job.id}/accept-suggestions`, 'POST')}
                      title="Guarda como respuesta, tal cual, cada sugerencia de la IA que sale de la documentación; las suposiciones quedan pendientes para revisarlas una a una"
                    >
                      {documented === pendingOfRound
                        ? 'Aprobar todas las sugerencias'
                        : `Aprobar las ${documented === 1 ? 'que sale' : `${documented} que salen`} de la documentación`}
                    </Button>
                  )}
                  {pendingOfRound > 0 && (
                    <LaunchButton
                      size="sm"
                      variant={allSuggested ? 'ghost' : undefined}
                      icon={Sparkles}
                      disabled={suggesting}
                      onClick={() => suggest(`/question-rounds/${view.job.id}/suggestions`)}
                      title="Pide a la IA una respuesta para cada pregunta pendiente de la ronda; no se guarda nada hasta que la guardes tú"
                    >
                      Sugerir respuestas
                    </LaunchButton>
                  )}
                  <Button variant="ghost" size="sm" icon={Terminal} onClick={() => onShowJob(view.job.id)} aria-label="Ver la ejecución" title="Ver la ejecución" />
                </>
              }
            >
              {shown.length === 0 ? (
                <p className="text-sm text-muted">
                  {ofRound.length === 0
                    ? finished
                      ? 'Esta ronda no dio preguntas.'
                      : 'Los modelos están preparando las preguntas.'
                    : 'Ninguna pregunta de esta ronda coincide con el filtro.'}
                </p>
              ) : (
                <ul className="divide-y divide-line">
                  {/* A new suggestion remounts the item, so its answer box opens filled with it. */}
                  {shown.map((q) => (
                    <QuestionItem
                      key={`${q.id}-${q.suggestedAt ?? ''}`}
                      question={q}
                      act={act}
                      suggesting={suggesting}
                      onSuggest={() => suggest(`/questions/${q.id}/suggestion`)}
                    />
                  ))}
                </ul>
              )}
            </Collapsible>
          )
        })}
      </div>
    </div>
  )
}

// Which output of the round counted: the arbiter's union, or one model alone.
function RoundResult({ jobId }: { jobId: string }) {
  const [result, setResult] = useState<PhaseResult | null>(null)

  useEffect(() => {
    fetchJson<PhaseResult>(`/jobs/${jobId}/result`)
      .then(setResult)
      .catch(() => setResult(null))
  }, [jobId])

  if (!result) return null
  // The usual case (the arbiter joined both proposals) says nothing; only the exceptions show.
  if (result.fuente === null) return <Badge tone="danger">Falló</Badge>
  return result.verificacion_cruzada ? null : <Badge tone="warn">Solo {result.fuente}, sin verificación</Badge>
}

type Act = (url: string, method: string, body?: unknown) => Promise<boolean>

// A question as one line (where it comes from, code, text and status) that
// opens to show why it is asked, its sources, the arbiter's note, the answer
// or the AI's suggestion, and the actions. A pending question with a
// suggestion opens with the answer box filled with it.
function QuestionItem({
  question: q,
  act,
  suggesting,
  onSuggest,
}: {
  question: Question
  act: Act
  suggesting: boolean
  onSuggest: () => void
}) {
  const suggestion = q.status === 'Pending' ? q.suggestedAnswer : null
  const [open, setOpen] = useState(false)
  const [mode, setMode] = useState<'view' | 'answer' | 'discard'>(suggestion !== null ? 'answer' : 'view')
  const [text, setText] = useState(q.answer ?? suggestion ?? '')
  // Whether the text in the box started from the AI's suggestion (the server
  // then records "ia" or "ia-editada" as the origin of the answer).
  const [fromSuggestion, setFromSuggestion] = useState(suggestion !== null)
  const [reason, setReason] = useState('')
  const status = STATUS[q.status]
  const [showNote, setShowNote] = useState(false)

  async function submit() {
    const ok =
      mode === 'answer'
        ? await act(`/questions/${q.id}/answer`, 'PUT', { answer: text, fromSuggestion })
        : await act(`/questions/${q.id}/discard`, 'POST', { reason })
    if (ok) setMode('view')
  }

  function fillWithSuggestion() {
    setText(suggestion ?? '')
    setFromSuggestion(true)
    setMode('answer')
  }

  const field = 'w-full rounded border border-line bg-surface px-3 py-2 text-sm placeholder:text-faint focus:outline-2 focus:outline-accent'

  return (
    <li>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        aria-expanded={open}
        className="flex w-full items-start gap-3 py-2.5 text-left"
      >
        <ChevronRight className={`mt-1 size-4 shrink-0 text-faint transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
        <Provenance origin={q.origin} />
        <span className="w-14 shrink-0 pt-0.5 text-sm font-medium text-muted">{q.code}</span>
        <span className={`min-w-0 flex-1 font-serif ${open ? '' : 'line-clamp-1'} ${q.status === 'Discarded' ? 'text-muted line-through decoration-faint' : ''}`}>
          {q.text}
        </span>
        <status.icon className={`mt-1 size-4 shrink-0 ${status.color}`} aria-label={status.label} />
        <span className="sr-only">{status.label}</span>
      </button>

      {open && (
        <div className="space-y-3 pb-4 pl-[4.75rem]">
          {/* The context of the question, small: it helps to answer but is not the answer. */}
          <div className="space-y-0.5 text-xs text-muted">
            <p>
              <span className="font-medium">Para qué:</span> {q.reason}
            </p>
            <p>
              <span className="font-medium">Fuentes:</span> {q.sources.join(', ') || 'ninguna'}
              {q.arbiterNote && (
                <>
                  {' '}
                  <button type="button" onClick={() => setShowNote(!showNote)} aria-expanded={showNote} className="ml-1 underline decoration-faint underline-offset-2 hover:text-fg">
                    {showNote ? 'Ocultar la nota del árbitro' : 'Nota del árbitro'}
                  </button>
                </>
              )}
            </p>
            {showNote && q.arbiterNote && <p className="max-w-prose pt-1">{q.arbiterNote}</p>}
            {q.status === 'Discarded' && q.discardReason && (
              <p>
                <span className="font-medium">Motivo del descarte:</span> {q.discardReason}
              </p>
            )}
          </div>

          {q.status === 'Answered' && mode === 'view' && (
            <div className="border-l-2 border-ok pl-3">
              <p className="font-serif">{q.answer}</p>
              {q.answerOrigin && <p className="mt-1 text-xs text-muted">{ANSWER_ORIGIN_NAMES[q.answerOrigin] ?? q.answerOrigin}</p>}
            </div>
          )}

          {/* The suggestion: on a pending question, next to the answer box; on an
              answered one that came from it, as the record of where it came from. */}
          {q.suggestedAnswer !== null &&
            (q.status === 'Pending' || (q.status === 'Answered' && (q.answerOrigin === 'ia' || q.answerOrigin === 'ia-editada'))) && (
              <SuggestionBox question={q} />
            )}

          {mode === 'answer' && (
            <textarea
              value={text}
              onChange={(e) => {
                setText(e.target.value)
                // Emptied box: whatever is written now is the user's own answer.
                if (e.target.value.trim() === '') setFromSuggestion(false)
              }}
              rows={3}
              placeholder="La respuesta del cliente"
              className={`${field} font-serif`}
            />
          )}
          {mode === 'discard' && (
            <input
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Motivo (opcional): innecesaria, fuera de alcance…"
              className={field}
            />
          )}

          <div className="flex flex-wrap gap-2">
            {mode !== 'view' ? (
              <>
                <Button variant="primary" size="sm" onClick={submit}>
                  {mode === 'answer' ? 'Guardar la respuesta' : 'Descartar la pregunta'}
                </Button>
                <Button size="sm" onClick={() => setMode('view')}>
                  Cancelar
                </Button>
                {mode === 'answer' && suggestion !== null && text !== suggestion && (
                  <Button variant="ghost" size="sm" icon={Sparkles} onClick={fillWithSuggestion}>
                    Volver a la sugerencia
                  </Button>
                )}
              </>
            ) : (
              <>
                {q.status !== 'Discarded' && (
                  <Button
                    size="sm"
                    onClick={() => {
                      setText(q.answer ?? '')
                      setFromSuggestion(false)
                      setMode('answer')
                    }}
                  >
                    {q.status === 'Answered' ? 'Editar la respuesta' : 'Responder'}
                  </Button>
                )}
                {suggestion !== null && (
                  <Button size="sm" icon={Sparkles} onClick={fillWithSuggestion}>
                    Usar la sugerencia
                  </Button>
                )}
                {q.status === 'Pending' && (
                  <LaunchButton size="sm" variant="ghost" icon={Sparkles} disabled={suggesting} onClick={onSuggest}>
                    {suggestion === null ? 'Sugerir con IA' : 'Pedir otra sugerencia'}
                  </LaunchButton>
                )}
                {q.status !== 'Discarded' && (
                  <Button size="sm" variant="ghost" onClick={() => setMode('discard')}>
                    Descartar
                  </Button>
                )}
                {q.status !== 'Pending' && (
                  <Button size="sm" variant="ghost" onClick={() => act(`/questions/${q.id}/reopen`, 'POST')}>
                    Reabrir
                  </Button>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </li>
  )
}

// The AI's suggestion and what it is based on: the documentation (with the
// fragments it cites) or a guess the user should confirm with the client.
// Folded by default: one line with its basis, that opens to show the
// suggested text, the fragments it cites and why.
function SuggestionBox({ question: q }: { question: Question }) {
  const [open, setOpen] = useState(false)
  const basis = q.suggestionBasis ? BASIS[q.suggestionBasis] : null
  return (
    <div className="text-sm">
      <button type="button" onClick={() => setOpen(!open)} aria-expanded={open} className="flex flex-wrap items-center gap-2 text-left text-muted hover:text-fg">
        <ChevronRight className={`size-3.5 transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
        <Sparkles className="size-3.5 text-accent" aria-hidden />
        <span>Sugerencia de la IA</span>
        {basis && <Badge tone={basis.tone}>{basis.label}</Badge>}
      </button>
      {open && (
        <div className="mt-2 rounded bg-sunken px-3 py-2.5">
          <p className="font-serif">{q.suggestedAnswer}</p>
          {q.suggestionNote && <p className="mt-1.5 text-xs text-muted">En qué se basa: {q.suggestionNote}</p>}
          {q.suggestionSources.length > 0 && <p className="mt-1 text-xs text-muted">Fuentes: {q.suggestionSources.join(', ')}</p>}
        </div>
      )}
    </div>
  )
}

export default QuestionsPanel
