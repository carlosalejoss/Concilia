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
  elements: {
    a: number | null
    b: number | null
    merged: number
    both: number
    onlyA: number
    onlyB: number
    discrepancies: number
    discarded: number
    // Step 88: elements where the arbiter went against the matching of the platform (null in older jobs).
    deviations: number | null
  } | null
  changes: { original: number; current: number; kept: number; modified: number; added: number; deleted: number } | null
  changesNote: string | null
  questions: { total: number; answered: number; discarded: number; pending: number; byUser: number; byAi: number; byAiEdited: number } | null
  arbiterModel: string | null
  rearbitrationOf: string | null
  // Step 95: the estimate of the runs without usage, and what the matching of the arbiter cost.
  estimatedInputTokens: number
  estimatedOutputTokens: number
  matchingTime: { seconds: number; cpuSeconds: number } | null
  // Step 93: requirements that cite a question answered with a guess of the AI taken as it was.
  assumedRequirements: number | null
  // Step 100: what the embeddings say of the same proposals (null without valid proposals of A and B).
  embeddings: {
    onlyA: number
    onlyB: number
    agreement: { total: number; rate: number } | null
  } | null
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
              {report.runsWithoutUsage === 1 ? '1 ejecución no tiene' : `${report.runsWithoutUsage} ejecuciones no tienen`} dato de consumo,
              porque se detuvieron antes de que Claude Code lo diera.
              {report.estimatedInputTokens > 0
                ? ` Estimado a partir de su consola: ${number(report.estimatedInputTokens)} tokens de entrada y ${number(report.estimatedOutputTokens)} de salida más.`
                : ' Sin consola guardada no se puede estimar: la cifra es un mínimo.'}
            </span>
          )}
          {report.matchingTime && (
            <span className="block text-xs text-muted">
              Emparejamiento por significado: {report.matchingTime.seconds.toLocaleString('es-ES', { maximumFractionDigits: 1 })} s
              ({report.matchingTime.cpuSeconds.toLocaleString('es-ES', { maximumFractionDigits: 1 })} s de CPU), en local y sin coste.
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
            de A y {elements.onlyB} solo de B; {elements.discrepancies} con discrepancia del árbitro y {elements.discarded} descartados.
            {elements.deviations !== null &&
              ` En ${elements.deviations} el árbitro se apartó del emparejamiento por significado de la plataforma.`}
            {report.assumedRequirements !== null &&
              report.assumedRequirements > 0 &&
              ` ${report.assumedRequirements} se apoyan en una suposición de la IA aceptada tal cual.`}
          </Line>
        )}
        {report.embeddings && (
          <Line label="Embeddings">
            Sin nada «mismo» ni «parecido» en la otra propuesta: {report.embeddings.onlyA} de A y {report.embeddings.onlyB} de B.
            {report.embeddings.agreement &&
              report.embeddings.agreement.total > 0 &&
              ` Coinciden con el árbitro en el ${(report.embeddings.agreement.rate * 100).toLocaleString('es-ES', { maximumFractionDigits: 0 })} % de los elementos (si cada uno está también en la otra propuesta).`}
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
