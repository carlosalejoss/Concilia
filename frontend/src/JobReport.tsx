import { useEffect, useState, type ReactNode } from 'react'

// The report of a job (step 64), as the API builds it from what the job
// left: the base of the evaluation of the TFG.
type Report = {
  succeeded: boolean
  inputTokens: number
  outputTokens: number
  runsWithoutUsage: number
  repairs: number
  seconds: number | null
  elements: { a: number | null; b: number | null; merged: number; both: number; onlyA: number; onlyB: number; discrepancies: number; discarded: number } | null
  changes: { original: number; current: number; kept: number; modified: number; added: number; deleted: number } | null
  changesNote: string | null
  questions: { total: number; answered: number; discarded: number; pending: number; byUser: number; byAi: number; byAiEdited: number } | null
  arbiterModel: string | null
  rearbitrationOf: string | null
}

const number = (n: number | null | undefined) => (n ?? 0).toLocaleString('es-ES')

function minutes(seconds: number | null): string {
  if (seconds === null) return '—'
  return seconds < 90 ? `${Math.round(seconds)} s` : `${Math.round(seconds / 60)} min`
}

// The figures of a finished job, in a few lines.
function JobReport({ jobId, finished }: { jobId: string; finished: boolean }) {
  const [report, setReport] = useState<Report | null>(null)

  useEffect(() => {
    if (!finished) return
    fetch(`/jobs/${jobId}/report`)
      .then((response) => (response.ok ? response.json() : null))
      .then(setReport)
      .catch(() => setReport(null))
  }, [jobId, finished])

  if (!report) return null
  const { elements, changes, questions } = report

  return (
    <details className="rounded border border-line bg-surface text-sm">
      <summary className="cursor-pointer px-3 py-2 font-medium">Informe del trabajo</summary>
      <dl className="grid gap-x-6 gap-y-2 border-t border-line px-3 py-3 sm:grid-cols-[auto_1fr]">
        <Line label="Coste">
          {number(report.inputTokens)} tokens de entrada y {number(report.outputTokens)} de salida · {minutes(report.seconds)} ·{' '}
          {report.repairs === 1 ? '1 reparación' : `${report.repairs} reparaciones`}
          {/* Runs stopped before Claude Code's last line leave no token counts: the figure is a minimum. */}
          {report.runsWithoutUsage > 0 && (
            <span className="block text-xs text-warn">
              Como mínimo: {report.runsWithoutUsage === 1 ? '1 ejecución no tiene' : `${report.runsWithoutUsage} ejecuciones no tienen`} dato de
              consumo, porque se detuvieron antes de que Claude Code lo diera.
            </span>
          )}
        </Line>
        {report.arbiterModel && (
          <Line label="Árbitro">
            {report.arbiterModel}
            {report.rearbitrationOf && ' · nuevo arbitraje de las mismas propuestas de A y B de otro trabajo'}
          </Line>
        )}
        {elements && (
          <Line label="Elementos">
            A propuso {elements.a ?? '—'} y B {elements.b ?? '—'}. La unión tiene {elements.merged}: {elements.both} de ambas, {elements.onlyA} solo
            de A y {elements.onlyB} solo de B; {elements.discrepancies} con discrepancia y {elements.discarded} descartados.
          </Line>
        )}
        {changes && (
          <Line label="Cambios del usuario">
            De {changes.original} a {changes.current}: {changes.kept} conservados, {changes.modified} modificados, {changes.added} añadidos y{' '}
            {changes.deleted} borrados.
          </Line>
        )}
        {report.changesNote && <Line label="Cambios del usuario">{report.changesNote}</Line>}
        {questions && (
          <Line label="Preguntas">
            {questions.total} en la ronda: {questions.answered} respondidas ({questions.byUser} por el usuario, {questions.byAi} con la sugerencia de
            la IA y {questions.byAiEdited} con la sugerencia editada), {questions.discarded} descartadas y {questions.pending} pendientes.
          </Line>
        )}
      </dl>
    </details>
  )
}

function Line({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-muted">{label}</dt>
      <dd>{children}</dd>
    </>
  )
}

export default JobReport
