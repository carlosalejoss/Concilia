import { useCallback, useEffect, useState } from 'react'
import { errorMessage, formatDate, type JobView, type Project, type Run, type RunEvent } from './api.ts'
import CatalogView from './CatalogView.tsx'

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

// The lots of one slot, each with its runs in attempt order (the first run,
// then its retries).
function lotsOfSlot(view: JobView, slot: string): Run[][] {
  const byLot = new Map<number, Run[]>()
  for (const run of view.runs.filter((r) => r.slot === slot)) {
    const lot = run.lotNumber ?? 1
    byLot.set(lot, [...(byLot.get(lot) ?? []), run])
  }
  return [...byLot.entries()]
    .sort(([a], [b]) => a - b)
    .map(([, runs]) => runs.sort((a, b) => a.attempt - b.attempt))
}

// One line for the list: the state that counts is the last attempt of each lot.
function slotSummary(view: JobView, slot: string): { text: string; state: string } {
  const finals = lotsOfSlot(view, slot).map((runs) => runs[runs.length - 1])
  if (finals.length === 0) return { text: '—', state: '' }
  const done = finals.filter((r) => r.state === 'Done').length
  const lots = finals.length > 1 ? ` (${done}/${finals.length} lotes)` : ''
  if (finals.some((r) => !isFinished(r))) {
    const running = finals.some((r) => r.state === 'Running')
    return { text: (running ? 'Running' : 'Queued') + lots, state: running ? 'Running' : 'Queued' }
  }
  if (done === finals.length) return { text: 'Done' + lots, state: 'Done' }
  const failed = finals.find((r) => r.state !== 'Done')!
  return { text: failed.state + lots, state: failed.state }
}

async function fetchJobs(projectId: number): Promise<JobView[]> {
  const response = await fetch(`/projects/${projectId}/jobs`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// Jobs of one project: a button that launches the catalog task to models A and
// B at the same time, the list of jobs and the A/B view of the selected one.
function Jobs({ project }: { project: Project }) {
  const [jobs, setJobs] = useState<JobView[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)

  // Reloads the list; also called when a run starts or ends. useCallback keeps
  // the same function between renders, so the consoles below do not reconnect
  // every time this component renders.
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
  const selectedUnfinished = selected?.runs.some((r) => !isFinished(r)) ?? false

  // While the selected job has queued or running runs, reload every 3 s: a
  // queued lot starts as soon as another lot of its slot ends, and its new
  // state must reach the page even if no console is open to announce it.
  useEffect(() => {
    if (!selectedUnfinished) return
    const timer = setInterval(reload, 3000)
    return () => clearInterval(timer)
  }, [selectedUnfinished, reload])

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Catálogos A/B</h2>

      <button
        type="button"
        onClick={start}
        disabled={starting}
        className="mt-4 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
      >
        Generar catálogo con A y B
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
                  const summary = slotSummary(view, slot)
                  return (
                    <td key={slot} className={`py-1 font-semibold ${stateColors[summary.state] ?? 'text-red-700'}`}>
                      {summary.text}
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
            <SlotColumn
              key={`${selected.job.id}-${slot}`}
              jobId={selected.job.id}
              slot={slot}
              lots={lotsOfSlot(selected, slot)}
              onChange={reload}
            />
          ))}
        </div>
      )}
    </section>
  )
}

// One side of a job: the merged catalog of its finished lots and, below, for
// each lot the live console of its run (retries included, they publish into
// the same stream) and the state of its last attempt.
function SlotColumn({
  jobId,
  slot,
  lots,
  onChange,
}: {
  jobId: string
  slot: string
  lots: Run[][]
  onChange: () => void
}) {
  const first = lots[0]?.[0]
  if (!first) {
    return null
  }

  // Changes each time a run of this slot finishes: the catalog is fetched again.
  const finishedRuns = lots.flat().filter(isFinished).length

  return (
    <div className="min-w-0">
      <h3 className="font-semibold">
        {slot} · {first.model} <span className="font-normal text-gray-500">({first.provider})</span>
      </h3>
      <CatalogView jobId={jobId} slot={slot} version={finishedRuns} />

      <details open className="mt-4">
        <summary className="cursor-pointer font-semibold">Ejecución por lotes</summary>
        {lots.map((runs) => {
          const lotRun = runs[0]
          const last = runs[runs.length - 1]
          return (
            <div key={lotRun.id} className="mt-3">
              {(lotRun.lotTotal ?? 1) > 1 && (
                <p className="text-sm font-semibold text-gray-600">
                  Lote {lotRun.lotNumber} de {lotRun.lotTotal}
                </p>
              )}
              {/* Queued lots have nothing to show yet: their console opens when they start. */}
              {lotRun.state !== 'Queued' ? (
                <RunConsole runId={lotRun.id} onFinished={onChange} />
              ) : (
                <p className="text-sm text-gray-500">En cola…</p>
              )}
              {isFinished(last) && <RunResult run={last} />}
            </div>
          )
        })}
      </details>
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

// Final state of a run. Its catalog is part of the merged one (CatalogView).
function RunResult({ run }: { run: Run }) {
  return (
    <div className="mt-2 text-sm">
      <p>
        Estado: <span className={`font-semibold ${stateColors[run.state] ?? 'text-red-700'}`}>{run.state}</span>
        {run.attempt > 1 && <> · Intento {run.attempt}</>}
        {run.verdictState && <> · Veredicto: {run.verdictState}</>}
        {run.numTurns !== null && <> · Turnos: {run.numTurns}</>}
      </p>
      {run.inputTokens !== null && (
        <p className="text-gray-500">
          Tokens: {run.inputTokens} de entrada, {run.outputTokens} de salida
        </p>
      )}
      <p className="text-gray-500">
        Prompt {run.promptVersion ?? '—'}
        {run.claudeCodeVersion && <> · Claude Code {run.claudeCodeVersion}</>}
      </p>
      {run.verdictReason && <p className="mt-1 text-amber-700">Motivo: {run.verdictReason}</p>}
      {run.errorMessage && <p className="mt-1 text-red-700">{run.errorMessage}</p>}
    </div>
  )
}

export default Jobs
