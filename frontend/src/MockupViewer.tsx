import { useEffect, useRef, useState } from 'react'
import { Download, ExternalLink, Maximize2, MonitorSmartphone } from 'lucide-react'
import { formatDate, type Deliverable, type JobView, type PhaseResult, type Project } from './api.ts'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import EmptyState from './ui/EmptyState.tsx'

// What the platform keeps about the current mockup (the .html itself is served
// apart, in GET /projects/{id}/mockup).
type MockupSummary = { historias: number; bytes: number; avisos: string[] }

// The mockup step: the current .html at full width in a sandboxed iframe,
// with its bar (download, open alone, full screen), and a notice when it is
// outdated or the last attempt to generate it failed. The page runs its own
// scripts but, without allow-same-origin, it cannot reach the application or
// its data; the server adds a CSP that also cuts it off from the network.
function MockupViewer({ project, version, onShowJob }: { project: Project; version: number; onShowJob: (jobId: string) => void }) {
  const [mockup, setMockup] = useState<Deliverable<MockupSummary> | null>(null)
  const [failedJob, setFailedJob] = useState<{ id: string; message: string } | null>(null)
  const frame = useRef<HTMLIFrameElement>(null)

  useEffect(() => {
    fetch(`/projects/${project.id}/deliverables/mockup`)
      .then((response) => (response.ok ? response.json() : null))
      .then(setMockup)
      .catch(() => setMockup(null))

    // The last mockup job: if it ended without a mockup, say why.
    fetch(`/projects/${project.id}/jobs`)
      .then((response) => (response.ok ? (response.json() as Promise<JobView[]>) : []))
      .then(async (jobs) => {
        const last = jobs.find((j) => j.job.kind === 'mockup')
        if (!last?.job.finishedAt) return setFailedJob(null)
        const result: PhaseResult | null = await fetch(`/jobs/${last.job.id}/result`).then((r) => (r.ok ? r.json() : null))
        setFailedJob(result && result.fuente === null ? { id: last.job.id, message: result.mensaje } : null)
      })
      .catch(() => setFailedJob(null))
  }, [project.id, version])

  const pageUrl = `/projects/${project.id}/mockup`
  // A new mockup changes the address, so the iframe loads it again.
  const stamp = mockup ? encodeURIComponent(mockup.updatedAt ?? mockup.createdAt) : ''

  return (
    <div className="space-y-3">
      {failedJob && (
        <Banner
          tone="danger"
          action={
            <Button variant="ghost" size="sm" onClick={() => onShowJob(failedJob.id)}>
              Ver la consola
            </Button>
          }
        >
          El último intento no dio un mockup válido: {failedJob.message}
        </Banner>
      )}
      {!mockup ? (
        <EmptyState icon={MonitorSmartphone} title="Todavía no hay mockup">
          Con el backlog validado, genera un prototipo navegable: una vista por historia, con datos de prueba, en un único .html.
        </EmptyState>
      ) : (
        <>
          {mockup.outdated && <Banner tone="stale">El backlog ha cambiado después de generar este mockup. Genéralo de nuevo para verlo al día.</Banner>}
          {mockup.content.avisos.length > 0 && (
            <Banner tone="warn">
              <ul className="list-disc pl-4">
                {mockup.content.avisos.map((warning) => (
                  <li key={warning}>{warning}</li>
                ))}
              </ul>
            </Banner>
          )}
          <div className="overflow-hidden rounded border border-line">
            <div className="flex flex-wrap items-center gap-2 border-b border-line bg-sunken px-3 py-2">
              <span className="mr-auto text-sm text-muted">
                {mockup.content.historias} historias, {Math.round(mockup.content.bytes / 1024)} KB. Generado el {formatDate(mockup.updatedAt ?? mockup.createdAt)}.
              </span>
              <Button variant="ghost" size="sm" icon={Maximize2} onClick={() => frame.current?.requestFullscreen()}>
                Pantalla completa
              </Button>
              <a href={pageUrl} target="_blank" rel="noopener noreferrer" className="inline-flex h-8 items-center gap-1.5 rounded px-2.5 text-sm font-medium text-muted hover:bg-surface hover:text-fg">
                <ExternalLink className="size-4" aria-hidden />
                Abrir aparte
              </a>
              <a href={`${pageUrl}?download=true`} className="inline-flex h-8 items-center gap-1.5 rounded bg-accent px-2.5 text-sm font-medium text-accent-fg hover:bg-accent-hover">
                <Download className="size-4" aria-hidden />
                Descargar el .html
              </a>
            </div>
            <iframe ref={frame} key={stamp} src={`${pageUrl}?v=${stamp}`} sandbox="allow-scripts" title={`Mockup de ${project.name}`} className="h-[75vh] w-full bg-white" />
          </div>
        </>
      )}
    </div>
  )
}

export default MockupViewer
