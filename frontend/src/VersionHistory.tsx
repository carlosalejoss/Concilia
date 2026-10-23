import { useEffect, useState } from 'react'
import { Eye, GitCompare, History, RotateCcw } from 'lucide-react'
import { errorMessage, formatDate, type DeliverableVersion } from './api.ts'
import Badge from './ui/Badge.tsx'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import Collapsible from './ui/Collapsible.tsx'
import Drawer from './ui/Drawer.tsx'
import { VersionContent, VersionDiff } from './VersionDetails.tsx'

function originLabel(v: DeliverableVersion): string {
  switch (v.origin) {
    case 'Existing':
      return 'Anterior al historial'
    case 'Generated':
      return 'Generada'
    case 'Edited':
      return 'Editada'
    case 'Validated':
      return 'Validada'
    case 'Restored':
      return `Vuelta a la versión ${v.restoredFrom}`
  }
}

// What the count of each kind is made of, and its name.
const KINDS: Record<string, { one: string; many: string; name: string }> = {
  catalog: { one: 'requisito', many: 'requisitos', name: 'catálogo' },
  backlog: { one: 'historia', many: 'historias', name: 'backlog' },
  mockup: { one: 'pantalla', many: 'pantallas', name: 'mockup' },
}

type VersionHistoryProps = {
  projectId: number
  // "catalog", "backlog" or "mockup".
  kind: string
  // Changes when the deliverable does, so the list is read again.
  refreshKey: string
  // There are unsaved changes: going back to a version would lose them.
  dirty?: boolean
  // A version was brought back: reload the deliverable and the flow.
  onRestored: () => void
}

// The history of a catalog, backlog or mockup (steps 101 and 102): a version
// each time it was generated, edited, validated or taken back, the newest
// first, with the one it has now marked. Each other one can be read, compared
// with the current one (not the mockup, which is opened instead) and brought
// back, as a draft: that is a new version, so it can be undone.
function VersionHistory({ projectId, kind, refreshKey, dirty = false, onRestored }: VersionHistoryProps) {
  const [versions, setVersions] = useState<DeliverableVersion[]>([])
  const [shown, setShown] = useState<{ number: number; compare: boolean } | null>(null)
  const [confirming, setConfirming] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch(`/projects/${projectId}/deliverables/${kind}/versions`)
      .then((response) => (response.ok ? (response.json() as Promise<DeliverableVersion[]>) : []))
      .then(setVersions)
      .catch(() => setVersions([]))
  }, [projectId, kind, refreshKey])

  async function restore(number: number) {
    setBusy(true)
    setError(null)
    try {
      const response = await fetch(`/projects/${projectId}/deliverables/${kind}/versions/${number}/restore`, { method: 'POST' })
      if (!response.ok) setError(await errorMessage(response))
      else {
        setConfirming(null)
        onRestored()
      }
    } catch (e: unknown) {
      setError(String(e))
    } finally {
      setBusy(false)
    }
  }

  if (versions.length === 0) return null
  const { one, many, name } = KINDS[kind]

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
      {error && <Banner tone="danger">{error}</Banner>}
      {dirty && <p className="mb-2 text-xs text-warn">Guarda o deshaz los cambios para volver a otra versión.</p>}
      <ol className="divide-y divide-line text-sm">
        {versions.map((v) => (
          <li key={v.number} className="space-y-1.5 py-1.5">
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <span className="w-20 font-medium">Versión {v.number}</span>
              <span className="w-48">{originLabel(v)}</span>
              <Badge tone={v.status === 'Validated' ? 'ok' : 'neutral'}>{v.status === 'Validated' ? 'Validada' : 'Borrador'}</Badge>
              {v.outdated && <Badge tone="stale">Desactualizada</Badge>}
              {v.elements !== null && (
                <span className="text-muted">
                  {v.elements} {v.elements === 1 ? one : many}
                </span>
              )}
              <span className="mr-auto text-muted">{formatDate(v.createdAt)}</span>
              {v.current ? (
                <Badge tone="accent">Actual</Badge>
              ) : (
                <span className="flex flex-wrap gap-1">
                  {kind === 'mockup' ? (
                    <a
                      href={`/projects/${projectId}/mockup/versions/${v.number}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex h-8 items-center gap-1.5 rounded px-2.5 text-sm font-medium text-muted hover:bg-sunken hover:text-fg"
                    >
                      <Eye className="size-4" aria-hidden />
                      Ver
                    </a>
                  ) : (
                    <>
                      <Button variant="ghost" size="sm" icon={Eye} onClick={() => setShown({ number: v.number, compare: false })}>
                        Ver
                      </Button>
                      <Button variant="ghost" size="sm" icon={GitCompare} onClick={() => setShown({ number: v.number, compare: true })}>
                        Comparar con la actual
                      </Button>
                    </>
                  )}
                  <Button variant="ghost" size="sm" icon={RotateCcw} disabled={dirty || busy} onClick={() => setConfirming(v.number)}>
                    Volver a esta versión
                  </Button>
                </span>
              )}
            </div>
            {confirming === v.number && (
              <Banner
                tone="warn"
                action={
                  <span className="flex gap-1">
                    <Button variant="ghost" size="sm" disabled={busy} onClick={() => setConfirming(null)}>
                      Cancelar
                    </Button>
                    <Button variant="primary" size="sm" disabled={busy || dirty} onClick={() => restore(v.number)}>
                      Volver a la versión {v.number}
                    </Button>
                  </span>
                }
              >
                {kind === 'mockup'
                  ? 'El mockup vuelve a esta versión. La actual queda en el historial, y se puede volver a ella.'
                  : `El ${name} vuelve a esta versión, en borrador: habrá que validarlo de nuevo. La versión actual queda en el historial, y se puede volver a ella. Los pasos siguientes quedan desactualizados.`}
              </Banner>
            )}
          </li>
        ))}
      </ol>
      <Drawer
        open={shown !== null}
        wide
        title={shown ? (shown.compare ? `De la versión ${shown.number} a la actual` : `Versión ${shown.number} del ${name}`) : ''}
        subtitle={shown && !shown.compare ? 'Solo lectura.' : undefined}
        onClose={() => setShown(null)}
      >
        {shown &&
          (shown.compare ? (
            <VersionDiff key={shown.number} projectId={projectId} kind={kind} number={shown.number} />
          ) : (
            <VersionContent key={shown.number} projectId={projectId} kind={kind} number={shown.number} />
          ))}
      </Drawer>
    </Collapsible>
  )
}

export default VersionHistory
