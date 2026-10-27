import { useEffect, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { errorMessage, type Deliverable, type Iteration } from './api.ts'
import { iterationsText } from './iterations.ts'
import Banner from './ui/Banner.tsx'
import LaunchButton from './ui/LaunchButton.tsx'

// Steps 113 and 114: "Actualizar con…" the validated catalog or backlog,
// with the iterations it does not include yet (marked by default). The other
// way is the button of the step, which generates it from scratch. The
// catalog lacks the closed iterations it does not include; the backlog, those
// its validated catalog already includes. Only while it is validated and
// lacks some iteration.
function UpdateLauncher({ kind, projectId, version, onLaunched }: { kind: 'catalog' | 'backlog'; projectId: number; version: number; onLaunched: () => void }) {
  const [missing, setMissing] = useState<number[]>([])
  const [iterations, setIterations] = useState<Iteration[]>([])
  const [chosen, setChosen] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [launching, setLaunching] = useState(false)

  useEffect(() => {
    Promise.all([fetch(`/projects/${projectId}/deliverables/catalog`), fetch(`/projects/${projectId}/deliverables/backlog`), fetch(`/projects/${projectId}/iterations`)])
      .then(async ([c, b, i]) => {
        const catalog: Deliverable<unknown> | null = c.ok ? await c.json() : null
        const backlog: Deliverable<unknown> | null = b.ok ? await b.json() : null
        let lacks: number[] = []
        if (kind === 'catalog' && catalog?.status === 'Validated') lacks = catalog.missingIterations ?? []
        if (kind === 'backlog' && backlog?.status === 'Validated' && catalog?.status === 'Validated')
          lacks = (catalog.iterations ?? []).filter((n) => !(backlog.iterations ?? []).includes(n))
        setMissing(lacks)
        setChosen(lacks)
        setIterations(i.ok ? await i.json() : [])
      })
      .catch(() => setMissing([]))
  }, [kind, projectId, version])

  if (missing.length === 0) return null
  const toggle = (n: number) => setChosen(chosen.includes(n) ? chosen.filter((x) => x !== n) : [...chosen, n].sort((a, b) => a - b))
  const nameOf = (n: number) => iterations.find((i) => i.number === n)?.name ?? `Iteración ${n}`
  const name = kind === 'catalog' ? 'catálogo' : 'backlog'

  async function launch() {
    setLaunching(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${projectId}/${kind}-update-jobs`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ iterations: chosen }),
      })
      if (!response.ok) setError(await errorMessage(response))
      else onLaunched()
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setLaunching(false)
    }
  }

  return (
    <div className="space-y-2 rounded border border-line bg-surface px-3 py-2.5">
      <p className="text-sm">
        {kind === 'catalog'
          ? `El catálogo validado no incluye ${iterationsText(missing)}. Puedes actualizarlo solo con lo nuevo, conservando lo validado`
          : `El catálogo validado ya incluye ${iterationsText(missing)}, pero el backlog no. Puedes actualizarlo solo con lo que ha cambiado en el catálogo, conservando lo validado`}{' '}
        (lo que la IA modifique o elimine lo revisas antes de validar), o generarlo desde cero con el botón del paso.
      </p>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
        <span className="text-muted">Actualizar con:</span>
        {missing.map((n) => (
          <label key={n} className="flex items-center gap-1.5">
            <input type="checkbox" checked={chosen.includes(n)} onChange={() => toggle(n)} className="accent-accent" />
            {nameOf(n)}
          </label>
        ))}
        <LaunchButton size="sm" icon={RefreshCw} disabled={chosen.length === 0 || launching} onClick={launch}>
          Actualizar el {name}
        </LaunchButton>
      </div>
      {error && <Banner tone="danger">{error}</Banner>}
    </div>
  )
}

export default UpdateLauncher
