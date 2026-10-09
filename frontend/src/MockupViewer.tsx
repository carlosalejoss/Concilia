import { useEffect, useState } from 'react'
import { type Deliverable, type JobView, type PhaseResult, type Project } from './api.ts'

// What the platform keeps about the current mockup (the .html itself is served
// apart, in GET /projects/{id}/mockup).
type MockupSummary = { historias: number; bytes: number; avisos: string[] }

// The mockup step (milestone 12): the current .html in a sandboxed iframe, the
// buttons to download it or open it alone, and a notice when it is outdated or
// the last attempt to generate it failed. The page runs its own scripts but,
// without allow-same-origin, it cannot reach the application or its data; the
// server adds a CSP that also cuts it off from the network.
function MockupViewer({
  project,
  version,
  onShowJob,
}: {
  project: Project
  version: number
  onShowJob: (jobId: string) => void
}) {
  const [mockup, setMockup] = useState<Deliverable<MockupSummary> | null>(null)
  const [failedJob, setFailedJob] = useState<{ id: string; message: string } | null>(null)

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
    <div className="mt-4 space-y-3 text-sm">
      {failedJob && (
        <p className="text-red-700">
          El último intento no dio un mockup válido: {failedJob.message}{' '}
          <button type="button" onClick={() => onShowJob(failedJob.id)} className="underline">
            Ver ejecución
          </button>
        </p>
      )}
      {mockup && (
        <>
          {mockup.outdated && (
            <p className="rounded bg-orange-50 p-2 text-orange-800">
              El backlog ha cambiado después de generar este mockup: genéralo de nuevo para verlo al día.
            </p>
          )}
          <div className="flex flex-wrap items-center gap-3">
            <span className="text-gray-600">
              {mockup.content.historias} historias · {Math.round(mockup.content.bytes / 1024)} KB · generado el{' '}
              {new Date(mockup.updatedAt ?? mockup.createdAt).toLocaleString('es-ES')}
            </span>
            <a href={`${pageUrl}?download=true`} className="rounded bg-blue-600 px-3 py-1 text-white">
              Descargar .html
            </a>
            <a href={pageUrl} target="_blank" rel="noopener noreferrer" className="rounded border border-gray-400 px-3 py-1">
              Abrir en otra pestaña
            </a>
          </div>
          {mockup.content.avisos.length > 0 && (
            <ul className="list-disc pl-5 text-amber-700">
              {mockup.content.avisos.map((warning) => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          )}
          <iframe
            key={stamp}
            src={`${pageUrl}?v=${stamp}`}
            sandbox="allow-scripts"
            title={`Mockup de ${project.name}`}
            className="h-[600px] w-full rounded border border-gray-300 bg-white"
          />
        </>
      )}
    </div>
  )
}

export default MockupViewer
