import { useEffect, useState } from 'react'
import { Terminal } from 'lucide-react'
import { formatDate, JOB_KIND_NAMES, type FlowStep, type JobView, type Project } from './api.ts'
import { isFinished, STEP_JOB_KINDS } from './jobs.ts'
import { SlotStates } from './Jobs.tsx'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import Collapsible from './ui/Collapsible.tsx'

// "Actividad de este paso": the jobs of the step (newest first) with the
// state of each run, folded at the bottom of the step. Each one opens in the
// technical view with its consoles. version changes when the flow reloads
// (every 3 s while something is running), so the states stay current.
function StepActivity({ project, step, version, onShowJob }: { project: Project; step: FlowStep; version: number; onShowJob: (jobId: string) => void }) {
  const [jobs, setJobs] = useState<JobView[]>([])
  const kinds = STEP_JOB_KINDS[step]

  useEffect(() => {
    if (kinds.length === 0) return
    fetch(`/projects/${project.id}/jobs`)
      .then(async (response) => (response.ok ? ((await response.json()) as JobView[]) : []))
      .then((all) => setJobs(all.filter((j) => kinds.includes(j.job.kind))))
      .catch(() => setJobs([]))
  }, [project.id, version, kinds])

  if (kinds.length === 0 || jobs.length === 0) return null
  const running = jobs.filter((j) => !j.runs.every(isFinished)).length

  return (
    <Collapsible
      persistKey={`p${project.id}.actividad.${step}`}
      title="Actividad de este paso"
      meta={
        <>
          <Badge>{jobs.length === 1 ? '1 ejecución' : `${jobs.length} ejecuciones`}</Badge>
          {running > 0 && <Badge tone="info">{running} en curso</Badge>}
        </>
      }
    >
      <ul className="divide-y divide-line">
        {jobs.map((view) => (
          <li key={view.job.id} className="flex flex-wrap items-center gap-x-4 gap-y-1 py-2">
            <span className="w-48 text-sm font-medium">{JOB_KIND_NAMES[view.job.kind] ?? view.job.kind}</span>
            <span className="w-36 text-sm text-muted">{formatDate(view.job.createdAt)}</span>
            <span className="flex-1">
              <SlotStates view={view} />
            </span>
            <Button variant="ghost" size="sm" icon={Terminal} onClick={() => onShowJob(view.job.id)}>
              Ver la consola
            </Button>
          </li>
        ))}
      </ul>
    </Collapsible>
  )
}

export default StepActivity
