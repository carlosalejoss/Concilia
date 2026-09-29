import { useCallback, useEffect, useState } from 'react'
import {
  errorMessage,
  formatDate,
  type JobView,
  type Project,
  type Run,
  type RunDetails,
  type RunEvent,
} from './api.ts'

const SLOTS = ['A', 'B'] as const

const stateColors: Record<string, string> = {
  Done: 'text-green-700',
  Blocked: 'text-amber-700',
  Queued: 'text-gray-500',
  Running: 'text-blue-700',
}

function isFinished(run: Run): boolean {
  return run.state !== 'Queued' && run.state !== 'Running'
}

// The runs of one slot in attempt order: the first run, then its retries.
function runsOfSlot(view: JobView, slot: string): Run[] {
  return view.runs.filter((r) => r.slot === slot).sort((a, b) => a.attempt - b.attempt)
}

async function fetchJobs(projectId: number): Promise<JobView[]> {
  const response = await fetch(`/projects/${projectId}/jobs`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// Jobs of one project: a button that launches the summary task to models A
// and B at the same time, the list of jobs and the A/B view of the selected one.
function Jobs({ project }: { project: Project }) {
  const [jobs, setJobs] = useState<JobView[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)

  // Reloads the list; also called when a run ends, to get its final state
  // and any retries. useCallback keeps the same function between renders,
  // so the consoles below do not reconnect every time this component renders.
  const reload = useCallback(() => {
    fetchJobs(project.id)
      .then(setJobs)
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    reload()
  }, [reload])

  async function start() {
    setStarting(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${project.id}/jobs`, { method: 'POST' })
      if (!response.ok) {
        setError(await errorMessage(response))
        return
      }
      const created: JobView = await response.json()
      setJobs((current) => [created, ...current])
      setSelectedId(created.job.id)
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setStarting(false)
    }
  }

  const selected = jobs.find((j) => j.job.id === selectedId)

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Ejecuciones A/B</h2>

      <button
        type="button"
        onClick={start}
        disabled={starting}
        className="mt-4 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
      >
        Ejecutar resumen con A y B
      </button>

      {error && <p className="mt-2 text-red-700">{error}</p>}

      {jobs.length > 0 && (
        <table className="mt-4 w-full text-left text-sm">
          <thead className="text-gray-500">
            <tr>
              <th className="py-1">Creado</th>
              <th className="py-1">A</th>
              <th className="py-1">B</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {jobs.map((view) => (
              <tr
                key={view.job.id}
                onClick={() => setSelectedId(view.job.id)}
                className={`cursor-pointer hover:bg-gray-50 ${view.job.id === selectedId ? 'bg-blue-50' : ''}`}
              >
                <td className="py-1">{formatDate(view.job.createdAt)}</td>
                {SLOTS.map((slot) => {
                  // The state that counts is the one of the last attempt.
                  const last = runsOfSlot(view, slot).at(-1)
                  return (
                    <td key={slot} className={`py-1 font-semibold ${stateColors[last?.state ?? ''] ?? 'text-red-700'}`}>
                      {last ? `${last.state}${last.attempt > 1 ? ` (intento ${last.attempt})` : ''}` : '—'}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Two columns side by side on wide screens, stacked on narrow ones. */}
      {selected && (
        <div className="mt-6 grid grid-cols-1 gap-6 md:grid-cols-2">
          {SLOTS.map((slot) => (
            <SlotColumn key={`${selected.job.id}-${slot}`} slot={slot} runs={runsOfSlot(selected, slot)} onFinished={reload} />
          ))}
        </div>
      )}
    </section>
  )
}

// One side of a job: the live console of its run (retries included, they
// publish into the same stream) and the result of its last attempt.
function SlotColumn({ slot, runs, onFinished }: { slot: string; runs: Run[]; onFinished: () => void }) {
  const first = runs[0]
  const last = runs.at(-1)
  if (!first || !last) {
    return null
  }

  return (
    <div className="min-w-0">
      <h3 className="font-semibold">
        {slot} · {first.model} <span className="font-normal text-gray-500">({first.provider})</span>
      </h3>
      <RunConsole runId={first.id} onFinished={onFinished} />
      {isFinished(last) && <RunResult key={last.id} run={last} />}
    </div>
  )
}

// Live console of one run through Server-Sent Events.
function RunConsole({ runId, onFinished }: { runId: string; onFinished: () => void }) {
  const [events, setEvents] = useState<RunEvent[]>([])

  useEffect(() => {
    // EventSource is the browser's client for Server-Sent Events: it keeps the
    // connection open and calls onmessage for each "data:" line.
    const source = new EventSource(`/runs/${runId}/events`)

    function finish() {
      source.close()
      onFinished()
    }

    source.onmessage = (message) => {
      const runEvent: RunEvent = JSON.parse(message.data)
      setEvents((current) => [...current, runEvent])
    }
    source.addEventListener('end', finish) // The API sends "event: end" when the run is over.
    source.onerror = finish // Without this, EventSource would reconnect forever.

    return () => source.close()
  }, [runId, onFinished])

  return (
    <div className="mt-2 max-h-80 overflow-auto rounded border border-gray-200 bg-gray-900 p-3 font-mono text-xs text-gray-100">
      {events.length === 0 && <p className="text-gray-400">Esperando eventos…</p>}
      {events.map((runEvent) => (
        <p key={runEvent.index} className="whitespace-pre-wrap break-words">
          <span className="text-gray-400">[{runEvent.kind}]</span> {runEvent.text}
        </p>
      ))}
    </div>
  )
}

// Final state of a run and the summary it wrote, if any.
function RunResult({ run }: { run: Run }) {
  const [summary, setSummary] = useState<string | null>(null)

  useEffect(() => {
    fetch(`/runs/${run.id}`)
      .then((response) => response.json())
      .then((body: RunDetails) => setSummary(body.summary))
      .catch(() => {})
  }, [run.id])

  return (
    <div className="mt-4">
      <p>
        Estado: <span className={`font-semibold ${stateColors[run.state] ?? 'text-red-700'}`}>{run.state}</span>
        {run.attempt > 1 && <> · Intento {run.attempt}</>}
        {run.verdictState && <> · Veredicto: {run.verdictState}</>}
        {run.numTurns !== null && <> · Turnos: {run.numTurns}</>}
      </p>
      {run.inputTokens !== null && (
        <p className="text-sm text-gray-500">
          Tokens: {run.inputTokens} de entrada, {run.outputTokens} de salida
        </p>
      )}
      {run.verdictReason && <p className="mt-1 text-amber-700">Motivo: {run.verdictReason}</p>}
      {run.errorMessage && <p className="mt-1 text-red-700">{run.errorMessage}</p>}
      {summary && (
        <pre className="mt-2 max-h-96 overflow-auto whitespace-pre-wrap rounded border border-gray-200 bg-gray-50 p-3 text-sm">
          {summary}
        </pre>
      )}
    </div>
  )
}

export default Jobs
