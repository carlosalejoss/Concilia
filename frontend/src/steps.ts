import type { FlowStep, StepState, StepStatus } from './api.ts'

// The names of the steps and of their states, shared by the bar of steps and the step header.
export const STEP_NAMES: Record<FlowStep, string> = {
  Documents: 'Documentos',
  Questions: 'Preguntas',
  Catalog: 'Requisitos',
  Backlog: 'Backlog',
  Mockup: 'Mockup',
}

const STATE_NAMES: Record<StepState, string> = {
  Blocked: 'Bloqueado',
  Available: 'Disponible',
  InProgress: 'En curso',
  Draft: 'Borrador',
  Validated: 'Validado',
}

// The name of a state, with the words that fit each step (documents and
// rounds have no "draft": they are there or not; the mockup is "generated").
export function stateName(step: StepStatus): string {
  if (step.step === 'Documents') return step.state === 'Draft' ? 'Con archivos' : 'Sin archivos'
  if (step.step === 'Questions' && step.state === 'Draft') return 'Con rondas'
  if (step.step === 'Mockup' && step.state === 'Draft') return 'Generado'
  return STATE_NAMES[step.state]
}
