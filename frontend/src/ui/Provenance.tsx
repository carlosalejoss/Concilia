// The one distinctive mark of the interface (design plan of 13-10-2026):
// where an element comes from. A thin vertical bar in two halves, the top one
// in the color of model A and the bottom one in that of model B, each lit only
// if that model proposed it. A discrepancy splits the bar in red. Shown next
// to every question, requirement and story, so the cross-check can be read at
// a glance.
export type Origin = 'A' | 'B' | 'ambas' | string | null | undefined

const LABELS: Record<string, string> = {
  A: 'Sale solo del modelo A',
  B: 'Sale solo del modelo B',
  ambas: 'Sale de los dos modelos',
}

function Provenance({ origin, discrepancy = false }: { origin: Origin; discrepancy?: boolean }) {
  const fromA = origin === 'A' || origin === 'ambas'
  const fromB = origin === 'B' || origin === 'ambas'
  const label = (LABELS[origin ?? ''] ?? 'Origen desconocido') + (discrepancy ? ', con discrepancia' : '')
  return (
    <span role="img" aria-label={label} title={label} className="inline-flex h-5 w-1 shrink-0 flex-col gap-px">
      <span className={`flex-1 rounded-t-full ${discrepancy ? 'bg-danger' : fromA ? 'bg-model-a' : 'bg-line'}`} />
      <span className={`flex-1 rounded-b-full ${discrepancy ? 'bg-danger opacity-50' : fromB ? 'bg-model-b' : 'bg-line'}`} />
    </span>
  )
}

export default Provenance
