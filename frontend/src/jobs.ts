import { SUGGESTIONS_KIND, type FlowStep, type JobView, type Run, type RunState } from './api.ts'
import type { Tone } from './ui/tones.ts'

// What the technical view of the runs and the activity of each step share:
// the slots of a job, the state of each slot and the color of each state.

// A and B in every job; the arbiter (ARB) only in the v0 phases. The answer
// suggestions (SUG) and the mockup (MOCK) have a single run.
export const SLOTS = ['A', 'B', 'ARB'] as const
export const SLOT_NAMES: Record<string, string> = { A: 'A', B: 'B', ARB: 'Árbitro', SUG: 'Sugerencias', MOCK: 'Mockup' }
const SINGLE_SLOT: Record<string, string> = { [SUGGESTIONS_KIND]: 'SUG', mockup: 'MOCK', rearbitration: 'ARB' }

export function slotsOf(view: JobView): string[] {
  const single = SINGLE_SLOT[view.job.kind]
  return single ? [single] : [...SLOTS]
}

// The jobs of each step of the flow, for the activity panel of the step.
export const STEP_JOB_KINDS: Record<FlowStep, string[]> = {
  Documents: [],
  Questions: ['questions', SUGGESTIONS_KIND],
  Catalog: ['catalog-v0'],
  Backlog: ['backlog'],
  Mockup: ['mockup'],
}

export const RUN_STATE_NAMES: Record<RunState, string> = {
  Queued: 'En cola',
  Running: 'En curso',
  Done: 'Hecho',
  Blocked: 'Bloqueado',
  Failed: 'Falló',
  TimedOut: 'Tiempo agotado',
  HeartbeatLost: 'Sin actividad',
  Skipped: 'No lanzado',
}

export const RUN_STATE_TONES: Record<RunState, Tone> = {
  Queued: 'neutral',
  Running: 'info',
  Done: 'ok',
  Blocked: 'warn',
  Failed: 'danger',
  TimedOut: 'danger',
  HeartbeatLost: 'danger',
  Skipped: 'warn',
}

export function isFinished(run: Run): boolean {
  return run.state !== 'Queued' && run.state !== 'Running'
}

// The lots of one slot, each with its runs in attempt order (the first run,
// then its retries).
export function lotsOfSlot(view: JobView, slot: string): Run[][] {
  const byLot = new Map<number, Run[]>()
  for (const run of view.runs.filter((r) => r.slot === slot)) {
    const lot = run.lotNumber ?? 1
    byLot.set(lot, [...(byLot.get(lot) ?? []), run])
  }
  return [...byLot.entries()]
    .sort(([a], [b]) => a - b)
    .map(([, runs]) => runs.sort((a, b) => a.attempt - b.attempt))
}

// The name of each run of a mockup job (prompts v0.8), a "lot" of the slot
// MOCK: the design (lot 1), each screen (up to lotTotal) and, after them, the
// fixes of the parts the platform found problems in.
export function mockupStepName(run: Run): string {
  const total = run.lotTotal ?? 0
  const lot = run.lotNumber ?? 1
  if (lot === 1) return 'Diseño: mapa de pantallas y armazón'
  if (lot <= total) return `Pantalla ${lot - 1} de ${total - 1}`
  return `Corrección ${lot - total}`
}

// The state of a slot: that of the last attempt of each lot (the worst one
// if they differ), with "n/m lotes" when there are lots ("n/m pasos" in the mockup).
export function slotSummary(view: JobView, slot: string): { state: RunState | null; lots: string } {
  const finals = lotsOfSlot(view, slot).map((runs) => runs[runs.length - 1])
  if (finals.length === 0) return { state: null, lots: '' }
  const done = finals.filter((r) => r.state === 'Done').length
  const total = Math.max(finals.length, finals[0].lotTotal ?? 0)
  const lots = total > 1 ? ` ${done}/${total} ${slot === 'MOCK' ? 'pasos' : 'lotes'}` : ''
  if (finals.some((r) => !isFinished(r))) return { state: finals.some((r) => r.state === 'Running') ? 'Running' : 'Queued', lots }
  if (done === finals.length) return { state: 'Done', lots }
  return { state: finals.find((r) => r.state !== 'Done')!.state, lots }
}
