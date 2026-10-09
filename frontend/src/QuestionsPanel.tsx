import { useCallback, useEffect, useState } from 'react'
import {
  errorMessage,
  formatDate,
  SUGGESTIONS_KIND,
  type JobView,
  type PhaseResult,
  type Project,
  type Question,
  type Run,
} from './api.ts'

const SLOT_NAMES: Record<string, string> = { A: 'A', B: 'B', ARB: 'Árbitro' }
const ORIGIN_NAMES: Record<string, string> = { A: 'Solo A', B: 'Solo B', ambas: 'A y B' }

const ANSWER_ORIGIN_NAMES: Record<string, string> = {
  usuario: 'Escrita por el usuario',
  ia: 'Sugerencia de la IA aceptada',
  'ia-editada': 'Sugerencia de la IA editada',
}
const BASIS_STYLES: Record<string, { label: string; color: string }> = {
  documentacion: { label: 'Basada en la documentación', color: 'bg-green-100 text-green-800' },
  suposicion: { label: 'Suposición: confírmala con el cliente', color: 'bg-amber-100 text-amber-800' },
}

const STATUS_STYLES: Record<string, { label: string; color: string }> = {
  Pending: { label: 'Pendiente', color: 'bg-amber-100 text-amber-800' },
  Answered: { label: 'Respondida', color: 'bg-green-100 text-green-800' },
  Discarded: { label: 'Descartada', color: 'bg-gray-200 text-gray-700' },
}

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

// The questions step (milestone 9): the rounds of the project, each with the
// questions it added, a filter of pending ones and, for each question, its
// origin, the arbiter's note and the actions (answer, edit, discard, reopen).
// The AI can suggest answers (to one question or to all the pending ones of a
// round): a suggestion fills the answer box, but nothing is saved until the
// user saves it, edited or not.
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
  const [onlyPending, setOnlyPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
    load()
  }

  if (rounds.length === 0) return null
  const pending = questions.filter((q) => q.status === 'Pending').length
  // The jobs come newest first: if the last suggestions failed, it is told.
  const lastSuggestions = suggestionJobs[0]
  const lastFailed = lastSuggestions !== undefined && lastSuggestions.runs.every(isFinished)
    && lastSuggestions.runs.some((r) => r.state !== 'Done')

  return (
    <div className="mt-4">
      <div className="flex flex-wrap items-center gap-4 text-sm">
        <span className={pending > 0 ? 'font-semibold text-amber-800' : 'text-gray-600'}>
          {pending === 1 ? '1 pregunta pendiente' : `${pending} preguntas pendientes`}
          {pending > 0 && ': el catálogo no se puede generar hasta contestarlas o descartarlas.'}
        </span>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={onlyPending} onChange={(e) => setOnlyPending(e.target.checked)} />
          Solo pendientes
        </label>
      </div>
      {suggesting && <p className="mt-2 text-sm text-blue-700">Generando sugerencias de respuesta con IA…</p>}
      {!suggesting && lastFailed && (
        <p className="mt-2 text-sm text-red-700">
          La última petición de sugerencias no terminó bien; puedes pedirlas de nuevo.{' '}
          <button type="button" onClick={() => onShowJob(lastSuggestions.job.id)} className="underline">
            Ver ejecución
          </button>
        </p>
      )}
      {error && <p className="mt-2 text-sm text-red-700">{error}</p>}

      <div className="mt-3 space-y-4">
        {rounds.map((view, index) => {
          const ofRound = questions.filter((q) => q.jobId === view.job.id)
          const shown = onlyPending ? ofRound.filter((q) => q.status === 'Pending') : ofRound
          const pendingOfRound = ofRound.filter((q) => q.status === 'Pending').length
          return (
            <div key={view.job.id} className="rounded bg-gray-50 p-3">
              <RoundHeader view={view} number={rounds.length - index} onShowJob={onShowJob} />
              {pendingOfRound > 0 && (
                <button
                  type="button"
                  disabled={suggesting}
                  onClick={() => suggest(`/question-rounds/${view.job.id}/suggestions`)}
                  className="mt-2 rounded border border-purple-300 bg-purple-50 px-3 py-1 text-sm text-purple-800 disabled:opacity-50"
                >
                  Sugerir con IA {pendingOfRound === 1 ? 'la pendiente' : `las ${pendingOfRound} pendientes`} de esta ronda
                </button>
              )}
              {shown.length === 0 && view.runs.every(isFinished) && (
                <p className="mt-2 text-sm text-gray-500">
                  {ofRound.length === 0 ? 'Esta ronda no dio preguntas.' : 'Ninguna pendiente en esta ronda.'}
                </p>
              )}
              <ul className="mt-2 space-y-3">
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
            </div>
          )
        })}
      </div>
    </div>
  )
}

// One line per round: its number, date, the state of A, B and the arbiter and,
// when it ends, which output counted.
function RoundHeader({ view, number, onShowJob }: { view: JobView; number: number; onShowJob: (jobId: string) => void }) {
  const last = (slot: string) => view.runs.filter((r) => r.slot === slot).sort((a, b) => b.attempt - a.attempt)[0]
  const finished = view.runs.every(isFinished)
  return (
    <div className="flex flex-wrap items-center gap-3 text-sm">
      <span className="font-semibold">Ronda {number}</span>
      <span className="text-gray-500">{formatDate(view.job.createdAt)}</span>
      {['A', 'B', 'ARB'].map((slot) => (
        <span key={slot}>
          {SLOT_NAMES[slot]}: {last(slot)?.state ?? '—'}
        </span>
      ))}
      {finished && <RoundResult jobId={view.job.id} />}
      <button type="button" onClick={() => onShowJob(view.job.id)} className="text-blue-700 underline">
        Ver ejecución
      </button>
    </div>
  )
}

function RoundResult({ jobId }: { jobId: string }) {
  const [result, setResult] = useState<PhaseResult | null>(null)

  useEffect(() => {
    fetchJson<PhaseResult>(`/jobs/${jobId}/result`)
      .then(setResult)
      .catch(() => setResult(null))
  }, [jobId])

  if (!result) return null
  if (result.fuente === null) return <span className="text-red-700">Falló: {result.mensaje}</span>
  return result.verificacion_cruzada ? (
    <span className="text-green-700">Unión del árbitro</span>
  ) : (
    <span className="text-amber-700">Solo {result.fuente}, sin verificación cruzada</span>
  )
}

type Act = (url: string, method: string, body?: unknown) => Promise<boolean>

// A question with its state and actions. Pending: answer, ask the AI for a
// suggestion or discard. Answered: edit the answer, discard or reopen.
// Discarded: reopen. A pending question with a suggestion opens with the
// answer box filled with it, to save as it is, edit or ignore.
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
  const [mode, setMode] = useState<'view' | 'answer' | 'discard'>(suggestion !== null ? 'answer' : 'view')
  const [text, setText] = useState(q.answer ?? suggestion ?? '')
  // Whether the text in the box started from the AI's suggestion (the server
  // then records "ia" or "ia-editada" as the origin of the answer).
  const [fromSuggestion, setFromSuggestion] = useState(suggestion !== null)
  const [reason, setReason] = useState('')
  const status = STATUS_STYLES[q.status]

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

  return (
    <li className="rounded border border-gray-200 bg-white p-3">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-mono text-sm font-semibold">{q.code}</span>
        <span className={`rounded px-2 py-0.5 text-xs ${status.color}`}>{status.label}</span>
        <span className="rounded bg-blue-50 px-2 py-0.5 text-xs text-blue-800">{ORIGIN_NAMES[q.origin] ?? q.origin}</span>
        <span className="text-xs text-gray-500">{q.sources.join(', ')}</span>
      </div>
      <p className="mt-1">{q.text}</p>
      <p className="text-sm text-gray-600">Para qué: {q.reason}</p>
      {q.arbiterNote && (
        <details className="mt-1 text-sm text-gray-600">
          <summary className="cursor-pointer">Nota del árbitro</summary>
          <p className="mt-1">{q.arbiterNote}</p>
        </details>
      )}
      {q.status === 'Answered' && mode === 'view' && (
        <div className="mt-2 rounded bg-green-50 p-2 text-sm">
          <p>
            <span className="font-semibold">Respuesta:</span> {q.answer}
          </p>
          {q.answerOrigin && <p className="mt-1 text-xs text-gray-600">{ANSWER_ORIGIN_NAMES[q.answerOrigin] ?? q.answerOrigin}</p>}
        </div>
      )}
      {/* The suggestion: on a pending question, next to the answer box; on an
          answered one that came from it, as the record of where it came from. */}
      {q.suggestedAnswer !== null &&
        (q.status === 'Pending' || (q.status === 'Answered' && (q.answerOrigin === 'ia' || q.answerOrigin === 'ia-editada'))) && (
          <SuggestionBox question={q} />
        )}
      {q.status === 'Discarded' && q.discardReason && (
        <p className="mt-2 text-sm text-gray-600">Motivo del descarte: {q.discardReason}</p>
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
          placeholder="Respuesta del cliente"
          className="mt-2 w-full rounded border border-gray-300 p-2 text-sm"
        />
      )}
      {mode === 'discard' && (
        <input
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Motivo (opcional): innecesaria, fuera de alcance…"
          className="mt-2 w-full rounded border border-gray-300 p-2 text-sm"
        />
      )}

      <div className="mt-2 flex flex-wrap gap-2 text-sm">
        {mode !== 'view' ? (
          <>
            <button type="button" onClick={submit} className="rounded bg-blue-600 px-3 py-1 text-white">
              {mode === 'answer' ? 'Guardar respuesta' : 'Descartar'}
            </button>
            <button type="button" onClick={() => setMode('view')} className="rounded border border-gray-300 px-3 py-1">
              Cancelar
            </button>
            {mode === 'answer' && suggestion !== null && text !== suggestion && (
              <button type="button" onClick={fillWithSuggestion} className="rounded border border-purple-300 px-3 py-1 text-purple-800">
                Volver a la sugerencia
              </button>
            )}
          </>
        ) : (
          <>
            {q.status !== 'Discarded' && (
              <button
                type="button"
                onClick={() => {
                  setText(q.answer ?? '')
                  setFromSuggestion(false)
                  setMode('answer')
                }}
                className="rounded border border-gray-300 px-3 py-1"
              >
                {q.status === 'Answered' ? 'Editar respuesta' : 'Responder'}
              </button>
            )}
            {suggestion !== null && (
              <button type="button" onClick={fillWithSuggestion} className="rounded border border-purple-300 px-3 py-1 text-purple-800">
                Usar sugerencia
              </button>
            )}
            {q.status === 'Pending' && (
              <button
                type="button"
                disabled={suggesting}
                onClick={onSuggest}
                className="rounded border border-purple-300 bg-purple-50 px-3 py-1 text-purple-800 disabled:opacity-50"
              >
                {suggestion === null ? 'Sugerir respuesta con IA' : 'Pedir otra sugerencia'}
              </button>
            )}
            {q.status !== 'Discarded' && (
              <button type="button" onClick={() => setMode('discard')} className="rounded border border-gray-300 px-3 py-1">
                Descartar
              </button>
            )}
            {q.status !== 'Pending' && (
              <button type="button" onClick={() => act(`/questions/${q.id}/reopen`, 'POST')} className="rounded border border-gray-300 px-3 py-1">
                Reabrir
              </button>
            )}
          </>
        )}
      </div>
    </li>
  )
}

// The AI's suggestion and what it is based on: the documentation (with the
// fragments it cites) or a guess the user should confirm with the client.
function SuggestionBox({ question: q }: { question: Question }) {
  const basis = q.suggestionBasis ? BASIS_STYLES[q.suggestionBasis] : null
  return (
    <div className="mt-2 rounded border border-purple-200 bg-purple-50 p-2 text-sm">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-semibold text-purple-900">Sugerencia de la IA</span>
        {basis && <span className={`rounded px-2 py-0.5 text-xs ${basis.color}`}>{basis.label}</span>}
        {q.suggestionSources.length > 0 && <span className="text-xs text-gray-500">{q.suggestionSources.join(', ')}</span>}
        {q.suggestedAt && <span className="text-xs text-gray-500">{formatDate(q.suggestedAt)}</span>}
      </div>
      <p className="mt-1">{q.suggestedAnswer}</p>
      {q.suggestionNote && <p className="mt-1 text-xs text-gray-600">En qué se basa: {q.suggestionNote}</p>}
    </div>
  )
}

export default QuestionsPanel
