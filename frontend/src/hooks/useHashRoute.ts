import { useEffect, useState } from 'react'
import type { FlowStep } from '../api.ts'

// Where the user is, kept in the URL after the "#" so a reload (or a link)
// opens the same place, without a router library:
//   #/                                    no project
//   #/proyectos/27/catalogo               a step of a project
//   #/proyectos/27/ejecuciones[/<jobId>]  the technical view of the runs
export type StepSlug = 'documentos' | 'preguntas' | 'catalogo' | 'backlog' | 'mockup'

export const STEP_SLUGS: Record<FlowStep, StepSlug> = {
  Documents: 'documentos',
  Questions: 'preguntas',
  Catalog: 'catalogo',
  Backlog: 'backlog',
  Mockup: 'mockup',
}

export type Route =
  | { kind: 'home' }
  | { kind: 'step'; projectId: number; step: FlowStep | null } // null: the project's current step
  | { kind: 'runs'; projectId: number; jobId: string | null }

export function parseRoute(hash: string): Route {
  const parts = hash.replace(/^#\/?/, '').split('/').filter(Boolean)
  const projectId = Number(parts[1])
  if (parts[0] !== 'proyectos' || !Number.isInteger(projectId)) return { kind: 'home' }
  if (parts[2] === 'ejecuciones') return { kind: 'runs', projectId, jobId: parts[3] ?? null }
  const step = (Object.keys(STEP_SLUGS) as FlowStep[]).find((s) => STEP_SLUGS[s] === parts[2]) ?? null
  return { kind: 'step', projectId, step }
}

export function routeHash(route: Route): string {
  switch (route.kind) {
    case 'home':
      return '#/'
    case 'step':
      return `#/proyectos/${route.projectId}${route.step ? `/${STEP_SLUGS[route.step]}` : ''}`
    case 'runs':
      return `#/proyectos/${route.projectId}/ejecuciones${route.jobId ? `/${route.jobId}` : ''}`
  }
}

export function useHashRoute() {
  const [route, setRoute] = useState<Route>(() => parseRoute(window.location.hash))

  useEffect(() => {
    const onChange = () => setRoute(parseRoute(window.location.hash))
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  // Changing the hash adds an entry to the history, so "back" works.
  const navigate = (next: Route) => {
    window.location.hash = routeHash(next)
  }

  return [route, navigate] as const
}
