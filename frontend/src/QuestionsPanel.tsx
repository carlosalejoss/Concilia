import { useCallback, useEffect, useState } from 'react'
import { errorMessage, formatDate, type JobView, type PhaseResult, type Project, type Question, type Run } from './api.ts'

const SLOT_NAMES: Record<string, string> = { A: 'A', B: 'B', ARB: 'Árbitro' }
const ORIGIN_NAMES: Record<string, string> = { A: 'Solo A', B: 'Solo B', ambas: 'A y B' }

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
        setQuestions(list)
      })
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    load()
  }, [load, version])

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

  if (rounds.length === 0) return null
  const pending = questions.filter((q) => q.status === 'Pending').length

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
      {error && <p className="mt-2 text-sm text-red-700">{error}</p>}

      <div className="mt-3 space-y-4">
        {rounds.map((view, index) => {
          const ofRound = questions.filter((q) => q.jobId === view.job.id)
          const shown = onlyPending ? ofRound.filter((q) => q.status === 'Pending') : ofRound
          return (
            <div key={view.job.id} className="rounded bg-gray-50 p-3">
              <RoundHeader view={view} number={rounds.length - index} onShowJob={onShowJob} />
              {shown.length === 0 && view.runs.every(isFinished) && (
                <p className="mt-2 text-sm text-gray-500">
                  {ofRound.length === 0 ? 'Esta ronda no dio preguntas.' : 'Ninguna pendiente en esta ronda.'}
                </p>
              )}
              <ul className="mt-2 space-y-3">
                {shown.map((q) => (
                  <QuestionItem key={q.id} question={q} act={act} />
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

// A question with its state and actions. Pending: answer or discard. Answered:
// edit the answer, discard or reopen. Discarded: reopen.
function QuestionItem({ question: q, act }: { question: Question; act: Act }) {
  const [mode, setMode] = useState<'view' | 'answer' | 'discard'>('view')
  const [text, setText] = useState(q.answer ?? '')
  const [reason, setReason] = useState('')
  const status = STATUS_STYLES[q.status]

  async function submit() {
    const ok =
      mode === 'answer'
        ? await act(`/questions/${q.id}/answer`, 'PUT', { answer: text })
        : await act(`/questions/${q.id}/discard`, 'POST', { reason })
    if (ok) setMode('view')
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
        <p className="mt-2 rounded bg-green-50 p-2 text-sm">
          <span className="font-semibold">Respuesta:</span> {q.answer}
        </p>
      )}
      {q.status === 'Discarded' && q.discardReason && (
        <p className="mt-2 text-sm text-gray-600">Motivo del descarte: {q.discardReason}</p>
      )}

      {mode === 'answer' && (
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
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
          </>
        ) : (
          <>
            {q.status !== 'Discarded' && (
              <button
                type="button"
                onClick={() => {
                  setText(q.answer ?? '')
                  setMode('answer')
                }}
                className="rounded border border-gray-300 px-3 py-1"
              >
                {q.status === 'Answered' ? 'Editar respuesta' : 'Responder'}
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

export default QuestionsPanel
