import { useCallback, useEffect, useState } from 'react'
import { errorMessage, formatDate, type Project, type Run, type RunDetails, type RunEvent } from './api.ts'

const stateColors: Record<string, string> = {
  Done: 'text-green-700',
  Blocked: 'text-amber-700',
  Queued: 'text-gray-500',
  Running: 'text-blue-700',
}

async function fetchRuns(projectId: number): Promise<Run[]> {
  const response = await fetch(`/projects/${projectId}/runs`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// Runs of one project: a button to launch the summary task, the list of runs
// and the detail (live console and result) of the selected one.
function Runs({ project }: { project: Project }) {
  const [runs, setRuns] = useState<Run[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)

  // Reloads the list; also used when a run ends (retries appear as new runs).
  // useCallback keeps the same function between renders, so RunView's effect
  // does not restart every time this component renders.
  const reload = useCallback(() => {
    fetchRuns(project.id)
      .then(setRuns)
      .catch((e: unknown) => setError(String(e)))
  }, [project.id])

  useEffect(() => {
    reload()
  }, [reload])

  async function start() {
    setStarting(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${project.id}/runs`, { method: 'POST' })
      if (!response.ok) {
        setError(await errorMessage(response))
        return
      }
      const run: Run = await response.json()
      setRuns((current) => [run, ...current])
      setSelectedId(run.id)
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setStarting(false)
    }
  }

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Ejecuciones</h2>

      <button
        type="button"
        onClick={start}
        disabled={starting}
        className="mt-4 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
      >
        Ejecutar resumen
      </button>

      {error && <p className="mt-2 text-red-700">{error}</p>}

      {runs.length > 0 && (
        <table className="mt-4 w-full text-left text-sm">
          <thead className="text-gray-500">
            <tr>
              <th className="py-1">Creada</th>
              <th className="py-1">Modelo</th>
              <th className="py-1">Intento</th>
              <th className="py-1">Estado</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {runs.map((run) => (
              <tr
                key={run.id}
                onClick={() => setSelectedId(run.id)}
                className={`cursor-pointer hover:bg-gray-50 ${run.id === selectedId ? 'bg-blue-50' : ''}`}
              >
                <td className="py-1">{formatDate(run.createdAt)}</td>
                <td className="py-1">
                  {run.model} <span className="text-gray-500">({run.provider})</span>
                </td>
                <td className="py-1">{run.attempt}</td>
                <td className={`py-1 font-semibold ${stateColors[run.state] ?? 'text-red-700'}`}>{run.state}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* key: selecting another run creates a fresh RunView (new connection). */}
      {selectedId && <RunView key={selectedId} runId={selectedId} onFinished={reload} />}
    </section>
  )
}

// Live console of one run (Server-Sent Events) and, when it ends, its result.
function RunView({ runId, onFinished }: { runId: string; onFinished: () => void }) {
  const [events, setEvents] = useState<RunEvent[]>([])
  const [details, setDetails] = useState<RunDetails | null>(null)

  useEffect(() => {
    // EventSource is the browser's client for Server-Sent Events: it keeps the
    // connection open and calls onmessage for each "data:" line.
    const source = new EventSource(`/runs/${runId}/events`)

    function finish() {
      source.close()
      fetch(`/runs/${runId}`)
        .then((response) => response.json())
        .then((body: RunDetails) => setDetails(body))
        .catch(() => {})
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

  const run = details?.run

  return (
    <div className="mt-6">
      <h3 className="font-semibold">Consola</h3>
      <div className="mt-2 max-h-80 overflow-auto rounded border border-gray-200 bg-gray-900 p-3 font-mono text-xs text-gray-100">
        {events.length === 0 && <p className="text-gray-400">Esperando eventos…</p>}
        {events.map((runEvent) => (
          <p key={runEvent.index} className="whitespace-pre-wrap">
            <span className="text-gray-400">[{runEvent.kind}]</span> {runEvent.text}
          </p>
        ))}
      </div>

      {run && (
        <div className="mt-4">
          <h3 className="font-semibold">Resultado</h3>
          <p className="mt-2">
            Estado: <span className={`font-semibold ${stateColors[run.state] ?? 'text-red-700'}`}>{run.state}</span>
            {run.verdictState && <> · Veredicto: {run.verdictState}</>}
            {run.numTurns !== null && <> · Turnos: {run.numTurns}</>}
            {run.inputTokens !== null && (
              <>
                {' '}
                · Tokens: {run.inputTokens} de entrada, {run.outputTokens} de salida
              </>
            )}
          </p>
          {run.verdictReason && <p className="mt-1 text-amber-700">Motivo: {run.verdictReason}</p>}
          {run.errorMessage && <p className="mt-1 text-red-700">{run.errorMessage}</p>}
          {details.summary && (
            <pre className="mt-2 max-h-96 overflow-auto whitespace-pre-wrap rounded border border-gray-200 bg-gray-50 p-3 text-sm">
              {details.summary}
            </pre>
          )}
        </div>
      )}
    </div>
  )
}

export default Runs
