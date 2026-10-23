import { useEffect, useState } from 'react'
import {
  errorMessage,
  type BacklogContent,
  type CatalogContent,
  type CatalogRequirement,
  type DeliverableVersion,
  type ElementDiff,
  type Story,
} from './api.ts'
import Badge from './ui/Badge.tsx'
import Banner from './ui/Banner.tsx'

// A requirement or a story, as one line of a list: what tells it apart.
function Element({ element, kind }: { element: CatalogRequirement | Story; kind: string }) {
  if (kind === 'catalog') {
    const r = element as CatalogRequirement
    return (
      <p className="text-sm">
        <span className="font-medium">{r.id}</span> {r.texto}
      </p>
    )
  }
  const h = element as Story
  return (
    <div className="text-sm">
      <p>
        <span className="font-medium">{h.id}</span> {h.titulo}
      </p>
      <p className="text-muted">
        Como {h.como}, quiero {h.quiero}, para {h.para}. {h.criterios.length} criterios, {h.tareas.length} tareas.
      </p>
    </div>
  )
}

// Reads one address of the API as JSON, with the error to show if it fails.
function useJson<T>(url: string): { data: T | null; error: string | null } {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    fetch(url)
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorMessage(response))
        setData((await response.json()) as T)
      })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)))
  }, [url])
  return { data, error }
}

// Step 102: what a version of the catalog or the backlog had, to read only.
export function VersionContent({ projectId, kind, number }: { projectId: number; kind: string; number: number }) {
  const { data, error } = useJson<DeliverableVersion<CatalogContent & BacklogContent>>(`/projects/${projectId}/deliverables/${kind}/versions/${number}`)
  if (error) return <Banner tone="danger">{error}</Banner>
  if (!data?.content) return <p className="text-sm text-muted">Cargando…</p>

  if (kind === 'catalog')
    return (
      <ul className="space-y-2">
        {data.content.requisitos.map((r) => (
          <li key={r.id}>
            <Element element={r} kind={kind} />
          </li>
        ))}
      </ul>
    )
  return (
    <div className="space-y-4">
      {data.content.epicas.map((epic) => (
        <section key={epic.id}>
          <h3 className="font-medium">
            {epic.id} {epic.titulo}
          </h3>
          <ul className="mt-1 space-y-2 border-l border-line pl-3">
            {epic.historias.map((h) => (
              <li key={h.id}>
                <Element element={h} kind={kind} />
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  )
}

// Step 102: what changed from a version to the current catalog or backlog,
// paired by content as in the report of a job (the ids are renumbered when
// saving, so a "modified" one is the one with the same id and another content).
export function VersionDiff({ projectId, kind, number }: { projectId: number; kind: string; number: number }) {
  const { data, error } = useJson<ElementDiff<CatalogRequirement | Story>>(`/projects/${projectId}/deliverables/${kind}/versions/${number}/comparison`)
  if (error) return <Banner tone="danger">{error}</Banner>
  if (!data) return <p className="text-sm text-muted">Cargando…</p>
  const unit = kind === 'catalog' ? 'requisitos' : 'historias'
  const nothing = data.modified.length + data.added.length + data.deleted.length === 0

  return (
    <div className="space-y-5">
      <p className="text-sm text-muted">
        La versión {number} tiene {data.original} {unit} y la actual, {data.current}: {data.kept} iguales, {data.modified.length} modificados,{' '}
        {data.added.length} añadidos y {data.deleted.length} borrados. Se emparejan por su contenido, porque los ids se renumeran al guardar.
      </p>
      {nothing && <Banner tone="ok">Tienen el mismo contenido.</Banner>}
      {data.modified.length > 0 && (
        <section className="space-y-2">
          <h3 className="flex items-center gap-2 font-medium">
            Modificados <Badge tone="warn">{data.modified.length}</Badge>
          </h3>
          {data.modified.map(({ before, after }) => (
            <div key={after.id} className="space-y-1 rounded border border-line p-2">
              <p className="text-xs font-medium text-muted">En la versión {number}</p>
              <Element element={before} kind={kind} />
              <p className="text-xs font-medium text-muted">Ahora</p>
              <Element element={after} kind={kind} />
            </div>
          ))}
        </section>
      )}
      {data.added.length > 0 && (
        <section className="space-y-2">
          <h3 className="flex items-center gap-2 font-medium">
            Añadidos después <Badge tone="ok">{data.added.length}</Badge>
          </h3>
          {data.added.map((e) => (
            <Element key={e.id} element={e} kind={kind} />
          ))}
        </section>
      )}
      {data.deleted.length > 0 && (
        <section className="space-y-2">
          <h3 className="flex items-center gap-2 font-medium">
            Borrados después <Badge tone="danger">{data.deleted.length}</Badge>
          </h3>
          {data.deleted.map((e) => (
            <Element key={e.id} element={e} kind={kind} />
          ))}
        </section>
      )}
    </div>
  )
}
