import { useEffect, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { errorMessage, type CatalogContent, type Deliverable, type Iteration } from './api.ts'
import { iterationsText } from './iterations.ts'
import Banner from './ui/Banner.tsx'
import LaunchButton from './ui/LaunchButton.tsx'

// Step 113: "Actualizar con…" the validated catalog, with the closed
// iterations it does not include yet (marked by default). The other way is
// the button of the step, which generates it from scratch with everything.
// Only while the catalog is validated and lacks some iteration.
function CatalogUpdateLauncher({ projectId, version, onLaunched }: { projectId: number; version: number; onLaunched: () => void }) {
  const [catalog, setCatalog] = useState<Deliverable<CatalogContent> | null>(null)
  const [iterations, setIterations] = useState<Iteration[]>([])
  const [chosen, setChosen] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [launching, setLaunching] = useState(false)

  useEffect(() => {
    Promise.all([fetch(`/projects/${projectId}/deliverables/catalog`), fetch(`/projects/${projectId}/iterations`)])
      .then(async ([c, i]) => {
        const loaded: Deliverable<CatalogContent> | null = c.ok ? await c.json() : null
        setCatalog(loaded)
        setIterations(i.ok ? await i.json() : [])
        setChosen(loaded?.missingIterations ?? [])
      })
      .catch(() => setCatalog(null))
  }, [projectId, version])

  const missing = catalog?.missingIterations ?? []
  if (!catalog || catalog.status !== 'Validated' || missing.length === 0) return null
  const toggle = (n: number) => setChosen(chosen.includes(n) ? chosen.filter((x) => x !== n) : [...chosen, n].sort((a, b) => a - b))
  const nameOf = (n: number) => iterations.find((i) => i.number === n)?.name ?? `Iteración ${n}`

  async function launch() {
    setLaunching(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${projectId}/catalog-update-jobs`, {
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
        El catálogo validado no incluye {iterationsText(missing)}. Puedes actualizarlo solo con lo nuevo, conservando lo validado (lo que la IA
        modifique o elimine lo revisas antes de validar), o generarlo desde cero con el botón del paso.
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
          Actualizar el catálogo
        </LaunchButton>
      </div>
      {error && <Banner tone="danger">{error}</Banner>}
    </div>
  )
}

export default CatalogUpdateLauncher
