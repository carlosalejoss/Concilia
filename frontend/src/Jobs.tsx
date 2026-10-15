import { useCallback, useEffect, useState } from 'react'
import { Download, FlaskConical, Terminal } from 'lucide-react'
import { errorMessage, formatDate, isPhaseJob, JOB_KIND_NAMES, type JobView, type PhaseResult, type Project, type Run, type RunEvent } from './api.ts'
import CatalogView from './CatalogView.tsx'
import JobReport from './JobReport.tsx'
import { isFinished, lotsOfSlot, mockupStepName, RUN_STATE_NAMES, RUN_STATE_TONES, SLOT_NAMES, SLOTS, slotsOf, slotSummary } from './jobs.ts'
import Badge from './ui/Badge.tsx'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import Collapsible from './ui/Collapsible.tsx'
import EmptyState from './ui/EmptyState.tsx'

async function fetchJobs(projectId: number): Promise<JobView[]> {
  const response = await fetch(`/projects/${projectId}/jobs`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json()
}

// The state of each slot of a job, as small labels ("A Hecho", "Árbitro En curso").
export function SlotStates({ view }: { view: JobView }) {
  return (
    <span className="flex flex-wrap gap-1">
      {slotsOf(view).map((slot) => {
        const { state, lots } = slotSummary(view, slot)
        return (
          <Badge key={slot} tone={state ? RUN_STATE_TONES[state] : 'neutral'}>
            {SLOT_NAMES[slot]} {state ? RUN_STATE_NAMES[state].toLowerCase() : '—'}
            {lots}
          </Badge>
        )
      })}
    </span>
  )
}

// The technical view of the runs: every job of the project (the v0 phases,
// A ‖ B → arbiter or a single model, and the experimental catalog v1.4 by
// lots) and, for the selected one, its result and the live console of each
// run. initialJobId selects a job when the view opens (from "Ver ejecución").
function Jobs({ project, initialJobId }: { project: Project; initialJobId?: string | null }) {
  const [jobs, setJobs] = useState<JobView[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(initialJobId ?? null)
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
    <section className="space-y-5">
      <div>
        <h2 className="text-xl font-semibold">Ejecuciones</h2>
        <p className="mt-1 max-w-prose text-sm text-muted">
          Todos los trabajos del proyecto, con sus consolas, tokens y errores. Las fases se lanzan desde los pasos del proyecto.
        </p>
        {jobs.length > 0 && (
          <a href={`/projects/${project.id}/report.csv`} className="mt-2 inline-flex items-center gap-1.5 text-sm text-accent hover:underline">
            <Download className="size-4" aria-hidden />
            Descargar el informe del proyecto (CSV, un trabajo por fila)
          </a>
        )}
      </div>

      {error && <Banner tone="danger">{error}</Banner>}

      {jobs.length === 0 ? (
        <EmptyState icon={Terminal} title="Todavía no hay ejecuciones">
          Aparecen aquí en cuanto lanzas una fase desde los pasos.
        </EmptyState>
      ) : (
        <ul className="divide-y divide-line rounded border border-line bg-surface">
          {jobs.map((view) => {
            const selectedRow = view.job.id === selectedId
            return (
              <li key={view.job.id}>
                <button
                  type="button"
                  onClick={() => setSelectedId(selectedRow ? null : view.job.id)}
                  aria-expanded={selectedRow}
                  className={`flex w-full flex-wrap items-center gap-x-4 gap-y-1 px-3 py-2.5 text-left transition-colors ${selectedRow ? 'bg-accent-soft' : 'hover:bg-sunken'}`}
                >
                  <span className={`w-52 font-medium ${selectedRow ? 'text-accent' : ''}`}>{JOB_KIND_NAMES[view.job.kind] ?? view.job.kind}</span>
                  <span className="w-36 text-sm text-muted">{formatDate(view.job.createdAt)}</span>
                  <SlotStates view={view} />
                </button>
              </li>
            )
          })}
        </ul>
      )}

      {selected && isPhaseJob(selected.job) && (
        <div className="space-y-4">
          <JobResult key={`${selected.job.id}-${selected.runs.filter(isFinished).length}`} jobId={selected.job.id} finished={!selectedUnfinished} />
          <JobReport key={`report-${selected.job.id}-${selected.runs.filter(isFinished).length}`} jobId={selected.job.id} finished={!selectedUnfinished} />
          {/* Columns side by side on wide screens, stacked on narrow ones. */}
          <div className={`grid grid-cols-1 gap-5 ${slotsOf(selected).length > 1 ? 'lg:grid-cols-3' : ''}`}>
            {slotsOf(selected).map((slot) =>
              slot === 'MOCK' ? (
                <MockupSteps key={`${selected.job.id}-${slot}`} steps={lotsOfSlot(selected, slot)} onChange={reload} />
              ) : (
                <PhaseSlotColumn
                  key={`${selected.job.id}-${slot}`}
                  slot={slot}
                  runs={selected.runs.filter((r) => r.slot === slot).sort((a, b) => a.attempt - b.attempt)}
                  onChange={reload}
                />
              ),
            )}
          </div>
        </div>
      )}
      {selected && !isPhaseJob(selected.job) && (
        <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
          {SLOTS.filter((slot) => slot !== 'ARB').map((slot) => (
            <SlotColumn key={`${selected.job.id}-${slot}`} jobId={selected.job.id} slot={slot} lots={lotsOfSlot(selected, slot)} onChange={reload} />
          ))}
        </div>
      )}

      <Collapsible persistKey={`p${project.id}.ejecuciones.v14`} title="Catálogo v1.4 por lotes" meta={<Badge>Experimental</Badge>}>
        <div className="flex flex-wrap items-center gap-3">
          <Button icon={FlaskConical} onClick={start} disabled={starting}>
            Generar el catálogo v1.4
          </Button>
          <span className="text-sm text-muted">Por lotes y sin árbitro. Se conserva para el hito 7 y no forma parte del flujo.</span>
        </div>
      </Collapsible>
    </section>
  )
}

// One side of a job of the catalog v1.4: the merged catalog of its finished
// lots and, for each lot, the live console of its run (retries included, they
// publish into the same stream) and the state of its last attempt.
function SlotColumn({ jobId, slot, lots, onChange }: { jobId: string; slot: string; lots: Run[][]; onChange: () => void }) {
  const first = lots[0]?.[0]
  if (!first) return null

  // Changes each time a run of this slot finishes: the catalog is fetched again.
  const finishedRuns = lots.flat().filter(isFinished).length

  return (
    <div className="min-w-0 space-y-3">
      <SlotHeader slot={slot} run={first} />
      <CatalogView jobId={jobId} slot={slot} version={finishedRuns} />
      {lots.map((runs) => {
        const lotRun = runs[0]
        const last = runs[runs.length - 1]
        return (
          <div key={lotRun.id} className="space-y-2">
            {(lotRun.lotTotal ?? 1) > 1 && (
              <p className="text-sm font-medium text-muted">
                Lote {lotRun.lotNumber} de {lotRun.lotTotal}
              </p>
            )}
            {/* Queued lots have nothing to show yet: their console opens when they start. */}
            {lotRun.state !== 'Queued' ? <RunConsole runId={lotRun.id} onFinished={onChange} /> : <p className="text-sm text-muted">En cola…</p>}
            {isFinished(last) && <RunResult run={last} />}
          </div>
        )
      })}
    </div>
  )
}

function SlotHeader({ slot, run }: { slot: string; run: Run }) {
  return (
    <h3 className="flex flex-wrap items-baseline gap-x-2">
      <span className="font-semibold">{SLOT_NAMES[slot]}</span>
      <span className="text-sm text-muted">{run.model}</span>
      <span className="text-xs text-faint">{run.provider}</span>
    </h3>
  )
}

// One slot of a v0 phase job: the live console of its run (a repair publishes
// into the same stream) and the state of its last attempt. The arbiter waits
// in the queue until A and B end; it is skipped when one of them failed.
function PhaseSlotColumn({ slot, runs, onChange }: { slot: string; runs: Run[]; onChange: () => void }) {
  const first = runs[0]
  if (!first) return null
  const last = runs[runs.length - 1]

  return (
    <div className="min-w-0 space-y-2">
      <SlotHeader slot={slot} run={first} />
      {first.state === 'Queued' && <p className="text-sm text-muted">{slot === 'ARB' ? 'Espera a que terminen A y B…' : 'En cola…'}</p>}
      {first.state !== 'Queued' && first.state !== 'Skipped' && <RunConsole runId={first.id} onFinished={onChange} />}
      {isFinished(last) && <RunResult run={last} />}
    </div>
  )
}

// A mockup job (prompts v0.6): its steps in order (the design, each screen
// and the review of the whole), each one folded with its console and its
// result. The one in progress opens by itself.
function MockupSteps({ steps, onChange }: { steps: Run[][]; onChange: () => void }) {
  const first = steps[0]?.[0]
  if (!first) return null
  const total = first.lotTotal ?? 0

  return (
    <div className="min-w-0 space-y-2">
      <SlotHeader slot="MOCK" run={first} />
      {total === 0 && <p className="text-sm text-muted">Las pantallas se conocerán cuando termine el diseño.</p>}
      {steps.map((runs) => {
        const run = runs[0]
        const last = runs[runs.length - 1]
        return (
          <details key={run.id} open={last.state === 'Running'} className="rounded border border-line bg-surface">
            <summary className="flex cursor-pointer flex-wrap items-center gap-2 px-3 py-2 text-sm">
              <span className="font-medium">{mockupStepName(run)}</span>
              <Badge tone={RUN_STATE_TONES[last.state]}>{RUN_STATE_NAMES[last.state]}</Badge>
            </summary>
            <div className="space-y-2 border-t border-line px-3 py-3">
              {run.state === 'Queued' && <p className="text-sm text-muted">En cola…</p>}
              {run.state !== 'Queued' && run.state !== 'Skipped' && <RunConsole runId={run.id} onFinished={onChange} />}
              {isFinished(last) && <RunResult run={last} />}
            </div>
          </details>
        )
      })}
    </div>
  )
}

// The result of a v0 phase job: the output that counts and where it comes from.
function JobResult({ jobId, finished }: { jobId: string; finished: boolean }) {
  const [result, setResult] = useState<PhaseResult | null>(null)

  useEffect(() => {
    if (!finished) return
    fetch(`/jobs/${jobId}/result`)
      .then((response) => (response.ok ? response.json() : null))
      .then(setResult)
      .catch(() => setResult(null))
  }, [jobId, finished])

  if (!result) return null
  const text =
    result.fuente === null
      ? `El trabajo falló: ${result.mensaje}`
      : result.verificacion_cruzada
        ? 'Resultado: la unión del árbitro.'
        : result.fuente === 'SUG'
          ? 'Resultado: la salida de un solo modelo (sugerencias de respuesta), sin verificación cruzada.'
          : result.fuente === 'MOCK'
            ? `${result.mensaje} Se ve en el paso «Mockup».`
            : `Resultado: la salida de ${result.fuente}, sin verificación cruzada.`
  return (
    <div className="space-y-2">
      <Banner tone={result.fuente === null ? 'danger' : result.verificacion_cruzada || result.fuente === 'MOCK' ? 'ok' : 'warn'}>{text}</Banner>
      {result.salida !== null && (
        <details className="text-sm">
          <summary className="cursor-pointer text-muted hover:text-fg">Ver la salida en JSON</summary>
          <pre className="mt-2 max-h-96 overflow-auto rounded border border-line bg-sunken p-3 text-xs">{JSON.stringify(result.salida, null, 2)}</pre>
        </details>
      )}
    </div>
  )
}

// Live console of one run through Server-Sent Events. A terminal look in both
// modes: it is the raw output of Claude Code.
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
    <div className="max-h-80 overflow-auto rounded bg-[#101614] p-3 font-mono text-xs leading-relaxed text-[#d7e0dc]">
      {events.length === 0 && <p className="text-[#7d8b86]">Esperando eventos…</p>}
      {events.map((runEvent) => (
        <p key={runEvent.index} className="break-words whitespace-pre-wrap">
          <span className="text-[#7d8b86]">[{runEvent.kind}]</span> {runEvent.text}
        </p>
      ))}
    </div>
  )
}

// Final state of a run: state, attempt, verdict, turns, tokens, versions and,
// if any, the reason or the error.
function RunResult({ run }: { run: Run }) {
  return (
    <div className="space-y-1 text-sm">
      <p className="flex flex-wrap items-center gap-1.5">
        <Badge tone={RUN_STATE_TONES[run.state]}>{RUN_STATE_NAMES[run.state]}</Badge>
        {run.attempt > 1 && <Badge>Intento {run.attempt}</Badge>}
        {run.verdictState && <Badge>Veredicto {run.verdictState}</Badge>}
        {run.numTurns !== null && <Badge>{run.numTurns} turnos</Badge>}
      </p>
      {run.inputTokens !== null && (
        <p className="text-muted">
          {run.inputTokens.toLocaleString('es-ES')} tokens de entrada, {run.outputTokens?.toLocaleString('es-ES')} de salida
        </p>
      )}
      <p className="text-xs text-faint">
        Prompt {run.promptVersion ?? '—'}
        {run.claudeCodeVersion && `, Claude Code ${run.claudeCodeVersion}`}
      </p>
      {run.verdictReason && <p className="text-warn">Motivo: {run.verdictReason}</p>}
      {run.errorMessage && <p className="text-danger">{run.errorMessage}</p>}
    </div>
  )
}

export default Jobs
