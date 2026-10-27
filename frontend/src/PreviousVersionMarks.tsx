import Badge from './ui/Badge.tsx'
import Collapsible from './ui/Collapsible.tsx'
import { versionText, type MarkedElement, type PreviousComparison, type VersionMark } from './previousVersion.ts'

// Step 115: the marks of a generation from scratch against the version before
// it, in the editors of the catalog and the backlog (previousVersion.ts).

// The filter by mark: 'all' or one of them.
export function PreviousVersionFilter({ comparison, value, onChange }: { comparison: PreviousComparison; value: VersionMark | 'all'; onChange: (value: VersionMark | 'all') => void }) {
  const count = (mark: VersionMark) => comparison.elements.filter((e) => e.mark === mark).length
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value as VersionMark | 'all')}
      aria-label="Filtrar frente a la versión anterior"
      title={`Frente a ${versionText(comparison)}, por significado`}
      className="rounded border border-line bg-surface px-2.5 py-1.5 text-sm"
    >
      <option value="all">Frente a la versión {comparison.version}: todos</option>
      <option value="Same">Iguales ({count('Same')})</option>
      <option value="Changed">Cambiados ({count('Changed')})</option>
      <option value="New">Nuevos ({count('New')})</option>
    </select>
  )
}

// The mark of an element, with what it was before in the tooltip.
export function PreviousVersionBadge({ element }: { element: MarkedElement | null }) {
  if (!element) return null
  const before = element.previousId ? `Antes ${element.previousId}: «${element.previousText}»` : undefined
  if (element.mark === 'Same')
    return (
      <span title={before}>
        <Badge tone="ok">Igual que {element.previousId}</Badge>
      </span>
    )
  if (element.mark === 'Changed')
    return (
      <span title={before}>
        <Badge tone="info">Cambiado</Badge>
      </span>
    )
  return <Badge tone="accent">Nuevo</Badge>
}

// What a changed element was before, inside the open element.
export function PreviousVersionDetail({ element }: { element: MarkedElement | null }) {
  if (element?.mark !== 'Changed') return null
  return (
    <p className="rounded bg-info-soft px-3 py-2 text-xs">
      Antes era {element.previousId}: <span className="font-serif">«{element.previousText}»</span>
    </p>
  )
}

// The elements of before that are not in the generation, folded.
export function GoneElements({ comparison, persistKey, noun }: { comparison: PreviousComparison; persistKey: string; noun: string }) {
  if (comparison.gone.length === 0) return null
  return (
    <Collapsible persistKey={persistKey} title={`Desaparecidos frente a la versión ${comparison.version}`} meta={<Badge tone="warn">{comparison.gone.length}</Badge>}>
      <p className="mb-2 text-sm text-muted">
        {noun} de {versionText(comparison)} sin nada parecido en lo generado. Si alguno hace falta, añádelo a mano.
      </p>
      <ul className="divide-y divide-line">
        {comparison.gone.map((g) => (
          <li key={g.id} className="flex gap-3 py-2 text-sm">
            <span className="w-16 shrink-0 font-medium text-muted">{g.id}</span>
            <span className="min-w-0 flex-1 font-serif">{g.text}</span>
          </li>
        ))}
      </ul>
    </Collapsible>
  )
}
