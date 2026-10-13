import { AlertTriangle, CheckCircle2, Circle, Loader2, Lock, PencilLine, type LucideIcon } from 'lucide-react'
import type { FlowStep, StepState, StepStatus } from './api.ts'
import { STEP_NAMES, stateName } from './steps.ts'

const STATE_LOOK: Record<StepState, { icon: LucideIcon; color: string }> = {
  Blocked: { icon: Lock, color: 'text-faint' },
  Available: { icon: Circle, color: 'text-accent' },
  InProgress: { icon: Loader2, color: 'text-info' },
  Draft: { icon: PencilLine, color: 'text-warn' },
  Validated: { icon: CheckCircle2, color: 'text-ok' },
}

// A step "done" for the bar: validated, or with what it needs (documents
// uploaded, a round finished, a mockup generated).
function isDone(step: StepStatus): boolean {
  return step.state === 'Validated' || (step.state === 'Draft' && step.step !== 'Catalog' && step.step !== 'Backlog')
}

// The five steps in a row, with the state of each one; the current one is
// highlighted and any of them can be opened.
function StepBar({ steps, current, onSelect }: { steps: StepStatus[]; current: FlowStep; onSelect: (step: FlowStep) => void }) {
  return (
    <ol className="flex items-stretch gap-1 overflow-x-auto rounded-xl border border-line bg-surface p-1.5">
      {steps.map((step, index) => {
        const look = STATE_LOOK[step.state]
        const Icon = isDone(step) ? CheckCircle2 : look.icon
        const color = isDone(step) ? 'text-ok' : look.color
        const selected = step.step === current
        return (
          <li key={step.step} className="min-w-36 flex-1">
            <button
              type="button"
              onClick={() => onSelect(step.step)}
              aria-current={selected ? 'step' : undefined}
              className={`flex h-full w-full items-center gap-2.5 rounded-lg px-3 py-2 text-left transition-colors ${
                selected ? 'bg-accent-soft' : 'hover:bg-sunken'
              }`}
            >
              <Icon className={`size-5 shrink-0 ${color} ${step.state === 'InProgress' ? 'animate-spin' : ''}`} aria-hidden />
              <span className="min-w-0">
                <span className={`block truncate text-sm font-medium ${selected ? 'text-accent' : ''}`}>
                  <span className="text-faint">{index + 1}.</span> {STEP_NAMES[step.step]}
                </span>
                <span className="flex items-center gap-1 text-xs text-muted">
                  {stateName(step)}
                  {step.outdated && (
                    <AlertTriangle className="size-3.5 text-stale" aria-label="Desactualizado" />
                  )}
                </span>
              </span>
            </button>
          </li>
        )
      })}
    </ol>
  )
}

export default StepBar
