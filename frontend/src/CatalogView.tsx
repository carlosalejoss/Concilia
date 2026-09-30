import { useEffect, useState } from 'react'
import { errorMessage, type CatalogMetrics, type SlotCatalog, type SourceTrace } from './api.ts'

function percent(value: number | null): string {
  return value === null ? '—' : `${Math.round(value * 100)} %`
}

// "DOC-01#F002: «quote»", one line per source, for the tooltip.
function traceTooltip(trace: SourceTrace[]): string {
  return trace.map((t) => `${t.fragment}: ${t.quote ? `«${t.quote}»` : '(sin cita)'}`).join('\n')
}

// The metrics of the validator for the merged catalog.
function Metrics({ metrics }: { metrics: CatalogMetrics }) {
  const items: [string, string][] = [
    ['Requisitos', String(metrics.requisitos)],
    ['Preguntas abiertas', String(metrics.preguntas_abiertas)],
    ['Conformidad sintáctica', percent(metrics.conformidad_sintactica)],
    ['Calidad de redacción', percent(metrics.calidad_redaccion)],
    ['Coherencia de decisión', percent(metrics.coherencia_decision)],
    ['Trazabilidad', percent(metrics.trazabilidad)],
    ['Fidelidad de cifras', percent(metrics.fidelidad_cifras)],
    ['Requisitos sin errores', percent(metrics.requisitos_sin_errores)],
    ['Citas literales', metrics.citas_literales],
    ['Cobertura de fragmentos', metrics.cobertura_fragmentos],
    ['Proporción MASTeR', percent(metrics.proporcion_master)],
    ['Autoverificación coincide', metrics.autoverificacion_coincide ? 'sí' : 'no'],
  ]
  return (
    <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
      {items.map(([name, value]) => (
        <div key={name} className="flex justify-between gap-2 border-b border-gray-100">
          <dt className="text-gray-600">{name}</dt>
          <dd className="font-semibold">{value}</dd>
        </div>
      ))}
    </dl>
  )
}

// The catalog of one model in a job: its finished lots merged by the API
// (GET /jobs/{id}/catalogs/{slot}). `version` changes whenever a run of the
// job finishes, so the catalog is fetched again with the new lots.
function CatalogView({ jobId, slot, version }: { jobId: string; slot: string; version: number }) {
  const [data, setData] = useState<SlotCatalog | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false // an older answer must not overwrite a newer one
    fetch(`/jobs/${jobId}/catalogs/${slot}`)
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorMessage(response))
        return (await response.json()) as SlotCatalog
      })
      .then((catalog) => {
        if (cancelled) return
        setData(catalog)
        setError(null)
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(String(e))
      })
    return () => {
      cancelled = true
    }
  }, [jobId, slot, version])

  if (error) return <p className="mt-2 text-sm text-red-700">{error}</p>
  if (!data) return <p className="mt-2 text-sm text-gray-500">Cargando catálogo…</p>

  const { report, catalog } = data
  return (
    <div className="mt-2">
      <p className="text-sm text-gray-600">
        Lotes en el catálogo: {data.includedLots.length} de {data.lotTotal}
        {data.pendingLots.length > 0 && <> · En curso: {data.pendingLots.length}</>}
        {data.failedLots.length > 0 && <span className="text-red-700"> · Fallidos: {data.failedLots.join(', ')}</span>}
      </p>

      {!report || !catalog ? (
        <p className="mt-2 text-sm text-gray-500">Aún no ha terminado ningún lote.</p>
      ) : (
        <>
          <p className="mt-2 text-sm">
            Validador:{' '}
            <span className={`font-semibold ${report.valid ? 'text-green-700' : 'text-red-700'}`}>
              {report.valid ? 'válido' : `no válido (${report.errors.length} errores)`}
            </span>
            {report.warnings.length > 0 && <> · {report.warnings.length} avisos</>}
          </p>
          {(report.errors.length > 0 || report.warnings.length > 0) && (
            <details className="mt-1 text-xs">
              <summary className="cursor-pointer text-gray-600">Errores y avisos</summary>
              <ul className="mt-1 list-disc pl-5">
                {report.errors.map((e) => (
                  <li key={e} className="text-red-700">
                    {e}
                  </li>
                ))}
                {report.warnings.map((w) => (
                  <li key={w} className="text-amber-700">
                    {w}
                  </li>
                ))}
              </ul>
            </details>
          )}
          {report.metrics && <Metrics metrics={report.metrics} />}

          <h4 className="mt-4 font-semibold">Requisitos ({catalog.requirements.length})</h4>
          <div className="overflow-x-auto">
            <table className="mt-1 w-full text-left text-sm">
              <thead className="text-gray-500">
                <tr>
                  <th className="py-1 pr-2">Id</th>
                  <th className="py-1 pr-2">Notación</th>
                  <th className="py-1 pr-2">Oblig.</th>
                  <th className="py-1 pr-2">Texto</th>
                  <th className="py-1">Fuente</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 align-top">
                {catalog.requirements.map((r) => (
                  <tr key={r.id} title={traceTooltip(r.source_trace)}>
                    <td className="py-1 pr-2 font-mono whitespace-nowrap">{r.id}</td>
                    <td className="py-1 pr-2">
                      {r.notation}
                      <span className="block text-xs text-gray-500">{r.ears_pattern ?? r.master_template}</span>
                    </td>
                    <td className="py-1 pr-2">{r.obligation}</td>
                    <td className="py-1 pr-2">{r.text}</td>
                    <td className="py-1 font-mono text-xs whitespace-nowrap">
                      {r.source_trace.map((t) => (
                        <span key={t.fragment} className="block cursor-help underline decoration-dotted">
                          {t.fragment}
                        </span>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <h4 className="mt-4 font-semibold">Preguntas abiertas ({catalog.open_questions.length})</h4>
          <ul className="mt-1 space-y-1 text-sm">
            {catalog.open_questions.map((q) => (
              <li key={q.id} title={traceTooltip(q.source_trace)}>
                <span className="font-mono">{q.id}</span> {q.question}
                {q.related_requirements.length > 0 && (
                  <span className="text-gray-500"> ({q.related_requirements.join(', ')})</span>
                )}
              </li>
            ))}
          </ul>

          {catalog.discarded_statements.length > 0 && (
            <p className="mt-3 text-xs text-gray-500">Fragmentos descartados: {catalog.discarded_statements.length}</p>
          )}
        </>
      )}
    </div>
  )
}

export default CatalogView
