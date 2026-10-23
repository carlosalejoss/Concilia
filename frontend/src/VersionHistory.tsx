import { useEffect, useState } from 'react'
import { History } from 'lucide-react'
import { formatDate, type DeliverableVersion } from './api.ts'
import Badge from './ui/Badge.tsx'
import Collapsible from './ui/Collapsible.tsx'

const ORIGINS: Record<DeliverableVersion['origin'], string> = {
  Existing: 'Anterior al historial',
  Generated: 'Generada',
  Edited: 'Editada',
  Validated: 'Validada',
}

// What the count of each kind is made of.
const ELEMENTS: Record<string, [string, string]> = {
  catalog: ['requisito', 'requisitos'],
  backlog: ['historia', 'historias'],
  mockup: ['pantalla', 'pantallas'],
}

// The history of a catalog, backlog or mockup (step 101): a version each time
// it was generated, edited or validated, the newest first, with the one it has
// now marked. kind: "catalog", "backlog" or "mockup". refreshKey changes when
// the deliverable does, so the list is read again.
function VersionHistory({ projectId, kind, refreshKey }: { projectId: number; kind: string; refreshKey: string }) {
  const [versions, setVersions] = useState<DeliverableVersion[]>([])

  useEffect(() => {
    fetch(`/projects/${projectId}/deliverables/${kind}/versions`)
      .then((response) => (response.ok ? (response.json() as Promise<DeliverableVersion[]>) : []))
      .then(setVersions)
      .catch(() => setVersions([]))
  }, [projectId, kind, refreshKey])

  if (versions.length === 0) return null
  const [one, many] = ELEMENTS[kind]

  return (
    <Collapsible
      persistKey={`p${projectId}.historial.${kind}`}
      title={
        <span className="flex items-center gap-2">
          <History className="size-4 text-faint" aria-hidden />
          Historial de versiones
        </span>
      }
      meta={<Badge>{versions.length}</Badge>}
    >
      <ol className="divide-y divide-line text-sm">
        {versions.map((v) => (
          <li key={v.number} className="flex flex-wrap items-center gap-x-3 gap-y-1 py-1.5">
            <span className="w-20 font-medium">Versión {v.number}</span>
            <span className="w-40">{ORIGINS[v.origin]}</span>
            <Badge tone={v.status === 'Validated' ? 'ok' : 'neutral'}>{v.status === 'Validated' ? 'Validada' : 'Borrador'}</Badge>
            {v.elements !== null && (
              <span className="text-muted">
                {v.elements} {v.elements === 1 ? one : many}
              </span>
            )}
            <span className="mr-auto text-muted">{formatDate(v.createdAt)}</span>
            {v.current && <Badge tone="accent">Actual</Badge>}
          </li>
        ))}
      </ol>
    </Collapsible>
  )
}

export default VersionHistory
