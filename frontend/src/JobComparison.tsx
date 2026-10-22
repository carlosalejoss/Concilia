import { useState } from 'react'
import { Search } from 'lucide-react'
import { errorMessages } from './api.ts'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import type { Tone } from './ui/tones.ts'

// GET /jobs/{id}/comparison (step 100).
type Level = 'Same' | 'Similar' | 'Different'
type ComparedElement = {
  id: string
  text: string
  bestId: string | null
  similarity: number
  level: Level
  arbiterJoined: boolean
  agreesWithEmbeddings: boolean
}
type Row = {
  kind: 'Merged' | 'Discarded' | 'Unclassified'
  arbiterId: string | null
  arbiterText: string | null
  origin: 'A' | 'B' | 'ambas' | null
  a: ComparedElement[]
  b: ComparedElement[]
  discrepancy: boolean
  explanation: string | null
  deviations: string[]
  reason: string | null
  agreesWithEmbeddings: boolean
}
type Comparison = {
  phase: 'Questions' | 'Catalog' | 'Backlog'
  thresholds: { same: number; similar: number }
  counts: { a: number; b: number; onlyA: number; onlyB: number }
  agreement: { total: number; rate: number } | null
  rows: Row[]
}

const ELEMENTS: Record<Comparison['phase'], string> = { Questions: 'preguntas', Catalog: 'requisitos', Backlog: 'historias' }

const LEVELS: Record<Level, { name: string; tone: Tone }> = {
  Same: { name: 'mismo', tone: 'ok' },
  Similar: { name: 'parecido', tone: 'info' },
  Different: { name: 'distinto', tone: 'neutral' },
}

const FILTERS = [
  ['all', 'Todas las filas', () => true],
  ['ambas', 'De ambas', (r: Row) => r.origin === 'ambas'],
  ['A', 'Solo de A', (r: Row) => r.kind === 'Merged' && r.origin === 'A'],
  ['B', 'Solo de B', (r: Row) => r.kind === 'Merged' && r.origin === 'B'],
  ['discrepancy', 'Con discrepancia del árbitro', (r: Row) => r.discrepancy],
  ['deviation', 'Con desviación del emparejamiento', (r: Row) => r.deviations.length > 0],
  ['discarded', 'Descartados', (r: Row) => r.kind === 'Discarded'],
  ['unclassified', 'Sin clasificar', (r: Row) => r.kind === 'Unclassified'],
  ['disagree', 'El árbitro no coincide con los embeddings', (r: Row) => !r.agreesWithEmbeddings],
] as const

type FilterId = (typeof FILTERS)[number][0]

const PAGE = 50
const number = (n: number) => n.toLocaleString('es-ES', { maximumFractionDigits: 2, minimumFractionDigits: 2 })

// A, B and the arbiter of a job side by side (step 100): a row per element of
// the merge, with what it joined of A and of B, and a row per discarded
// element; next to each element of A and B, its closest element in the other
// proposal by meaning. It loads when it opens, since the first time an older
// job computes its embeddings (a few seconds); then they are saved.
function JobComparison({ jobId }: { jobId: string }) {
  const [comparison, setComparison] = useState<Comparison | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [filter, setFilter] = useState<FilterId>('all')
  const [search, setSearch] = useState('')
  const [shown, setShown] = useState(PAGE)

  async function load() {
    if (comparison || loading) return
    setLoading(true)
    setError(null)
    try {
      const response = await fetch(`/jobs/${jobId}/comparison`)
      if (!response.ok) throw new Error((await errorMessages(response)).join(' '))
      setComparison((await response.json()) as Comparison)
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setLoading(false)
    }
  }

  const rows = comparison?.rows ?? []
  const merged = rows.filter((r) => r.kind === 'Merged')
  const needle = search.trim().toLowerCase()
  const matches = FILTERS.find(([id]) => id === filter)![2]
  const visible = rows.filter(
    (r) =>
      matches(r) &&
      (needle === '' ||
        [r.arbiterId, r.arbiterText, r.reason, ...[...r.a, ...r.b].flatMap((e) => [e.id, e.text])].some((t) => t?.toLowerCase().includes(needle))),
  )

  return (
    <details className="rounded border border-line bg-surface text-sm" onToggle={(e) => e.currentTarget.open && void load()}>
      <summary className="cursor-pointer px-3 py-2 font-medium">Comparación de A, B y el árbitro</summary>
      <div className="space-y-3 border-t border-line px-3 py-3">
        {loading && <p className="text-muted">Cargando… La primera vez calcula los embeddings del trabajo, y puede tardar unos segundos.</p>}
        {error && <p className="text-danger">{error}</p>}
        {comparison && (
          <>
            <p className="max-w-prose text-muted">
              A propuso {comparison.counts.a} {ELEMENTS[comparison.phase]} y B {comparison.counts.b}. El árbitro dejó {merged.length}:{' '}
              {merged.filter((r) => r.origin === 'ambas').length} de ambas, {merged.filter((r) => r.origin === 'A').length} solo de A y{' '}
              {merged.filter((r) => r.origin === 'B').length} solo de B, y descartó {rows.filter((r) => r.kind === 'Discarded').length}. Según los
              embeddings, {comparison.counts.onlyA} de A y {comparison.counts.onlyB} de B no tienen nada «mismo» (desde{' '}
              {number(comparison.thresholds.same)}) ni «parecido» (desde {number(comparison.thresholds.similar)}) en la otra propuesta
              {comparison.agreement && comparison.agreement.total > 0
                ? `, y coinciden con el árbitro en el ${Math.round(comparison.agreement.rate * 100)} % de los elementos.`
                : '.'}
            </p>

            <div className="flex flex-wrap items-center gap-3">
              <label className="flex min-w-48 flex-1 items-center gap-2 rounded border border-line bg-surface px-2.5 py-1.5 focus-within:outline-2 focus-within:outline-accent">
                <Search className="size-4 text-faint" aria-hidden />
                <input
                  value={search}
                  onChange={(e) => {
                    setSearch(e.target.value)
                    setShown(PAGE)
                  }}
                  placeholder="Buscar por id o texto"
                  className="w-full bg-transparent placeholder:text-faint focus:outline-none"
                />
              </label>
              <select
                value={filter}
                onChange={(e) => {
                  setFilter(e.target.value as FilterId)
                  setShown(PAGE)
                }}
                aria-label="Filtrar las filas"
                className="rounded border border-line bg-surface px-2.5 py-1.5"
              >
                {FILTERS.map(([id, label, test]) => (
                  <option key={id} value={id}>
                    {label} ({rows.filter(test).length})
                  </option>
                ))}
              </select>
            </div>

            {visible.length === 0 ? (
              <p className="text-muted">Ninguna fila coincide con el filtro.</p>
            ) : (
              <div className="divide-y divide-line rounded border border-line">
                <div className="hidden grid-cols-3 gap-4 bg-sunken px-3 py-1.5 text-xs font-medium text-muted lg:grid">
                  <span>A</span>
                  <span>Árbitro</span>
                  <span>B</span>
                </div>
                {visible.slice(0, shown).map((r, i) => (
                  <ComparisonRow key={`${r.kind}-${r.arbiterId ?? ''}-${i}`} row={r} />
                ))}
              </div>
            )}
            {visible.length > shown && (
              <Button size="sm" onClick={() => setShown(shown + PAGE)}>
                Mostrar {Math.min(PAGE, visible.length - shown)} más ({visible.length - shown} sin mostrar)
              </Button>
            )}
          </>
        )}
      </div>
    </details>
  )
}

function ComparisonRow({ row: r }: { row: Row }) {
  return (
    <div className={`grid gap-4 px-3 py-3 lg:grid-cols-3 ${r.agreesWithEmbeddings ? '' : 'bg-warn-soft/30'}`}>
      <Side label="A" elements={r.a} />
      <div className="space-y-1.5">
        <p className="text-xs font-medium text-muted lg:hidden">Árbitro</p>
        <p className="flex flex-wrap items-center gap-1">
          {r.kind === 'Merged' && <span className="mr-1 font-medium text-muted">{r.arbiterId}</span>}
          {r.kind === 'Merged' && r.origin && <Badge tone="accent">{r.origin === 'ambas' ? 'De ambas' : `Solo de ${r.origin}`}</Badge>}
          {r.kind === 'Discarded' && <Badge>Descartado</Badge>}
          {r.kind === 'Unclassified' && <Badge tone="warn">Sin clasificar</Badge>}
          {r.discrepancy && <Badge tone="danger">Discrepancia</Badge>}
          {r.deviations.length > 0 && <Badge tone="danger">Desviación</Badge>}
          {!r.agreesWithEmbeddings && <Badge tone="warn">No coincide con los embeddings</Badge>}
        </p>
        {r.arbiterText && <p className="font-serif">{r.arbiterText}</p>}
        {r.reason && <p className="text-muted">Motivo: {r.reason}</p>}
        {r.kind === 'Unclassified' && <p className="text-muted">El árbitro no lo unió con nada ni lo descartó.</p>}
        {(r.explanation || r.deviations.length > 0) && (
          <details className="text-xs text-muted">
            <summary className="cursor-pointer hover:text-fg">Explicación</summary>
            {r.explanation && <p className="mt-1">{r.explanation}</p>}
            {r.deviations.map((d) => (
              <p key={d} className="mt-1">
                [Plataforma] {d}
              </p>
            ))}
          </details>
        )}
      </div>
      <Side label="B" elements={r.b} />
    </div>
  )
}

// The elements of one proposal in a row, each with its closest element in the other.
function Side({ label, elements }: { label: string; elements: ComparedElement[] }) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-medium text-muted lg:hidden">{label}</p>
      {elements.length === 0 ? (
        <p className="text-faint">—</p>
      ) : (
        elements.map((e) => (
          <div key={e.id} className="space-y-0.5">
            <p>
              <span className="mr-1.5 font-medium text-muted">{e.id}</span>
              <span className="font-serif">{e.text || 'Sin texto'}</span>
            </p>
            <p className="flex flex-wrap items-center gap-1 text-xs text-muted">
              <Badge tone={LEVELS[e.level].tone}>
                {LEVELS[e.level].name} {number(e.similarity)}
              </Badge>
              {e.bestId && <span>con {e.bestId} de la otra</span>}
            </p>
          </div>
        ))
      )}
    </div>
  )
}

export default JobComparison
