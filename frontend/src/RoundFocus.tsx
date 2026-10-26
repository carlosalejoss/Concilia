import { useEffect, useState } from 'react'
import type { Iteration } from './api.ts'
import { showIterations, useElementIterations } from './iterations.ts'

// Step 112: what a new round of questions works on, chosen before launching
// it: all the iterations (the round of always) or "Centrarse en…" some of
// them, closed and with documents. Only once the project has more than one
// iteration. value: the iterations it centres on, empty for all of them.
function RoundFocus({ projectId, version, value, onChange }: { projectId: number; version: number; value: number[]; onChange: (focus: number[]) => void }) {
  const map = useElementIterations(projectId, version)
  const [iterations, setIterations] = useState<Iteration[]>([])

  useEffect(() => {
    fetch(`/projects/${projectId}/iterations`)
      .then(async (response) => (response.ok ? setIterations(await response.json()) : setIterations([])))
      .catch(() => setIterations([]))
  }, [projectId, version])

  if (!showIterations(map)) return null
  const withDocuments = new Set(Object.values(map.documents))
  const choices = iterations.filter((i) => i.status === 'Closed' && withDocuments.has(i.number))
  const centred = value.length > 0
  const toggle = (n: number) => onChange(value.includes(n) ? value.filter((x) => x !== n) : [...value, n].sort((a, b) => a - b))

  return (
    <fieldset className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
      <legend className="sr-only">En qué iteraciones se centra la ronda</legend>
      <label className="flex items-center gap-1.5">
        <input type="radio" checked={!centred} onChange={() => onChange([])} className="accent-accent" />
        Todas las iteraciones
      </label>
      <label className="flex items-center gap-1.5" title="La ronda se fija en la documentación de las iteraciones que marques, con lo anterior que se le parece, y revisa las preguntas pendientes">
        <input
          type="radio"
          checked={centred}
          onChange={() => onChange(choices.length > 0 ? [choices[choices.length - 1].number] : [])}
          className="accent-accent"
        />
        Centrarse en…
      </label>
      {centred &&
        choices.map((i) => (
          <label key={i.id} className="flex items-center gap-1.5">
            <input type="checkbox" checked={value.includes(i.number)} onChange={() => toggle(i.number)} className="accent-accent" />
            {i.name}
          </label>
        ))}
    </fieldset>
  )
}

export default RoundFocus
