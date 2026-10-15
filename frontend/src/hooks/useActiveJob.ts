import { createContext, useContext } from 'react'

// GET /jobs/active: the task being executed now, in any project.
export type ActiveJob = {
  jobId: string
  projectId: number
  projectName: string
  kind: string
  task: string
  // "Se está ejecutando el mockup en el proyecto «…». Espera a que termine…"
  message: string
}

// One task at a time in the whole platform (15-10-2026): while one runs, the
// buttons that launch a task are greyed out everywhere and, if pressed, say
// which task is running and in which project (see LaunchButton).
export type ActiveJobState = {
  active: ActiveJob | null
  // Ask the API again now (after launching a task), without waiting for the next check.
  refresh: () => void
  // Show the message of the running task (a button that launches was pressed).
  warn: () => void
}

export const ActiveJobContext = createContext<ActiveJobState>({ active: null, refresh: () => {}, warn: () => {} })

export function useActiveJob(): ActiveJobState {
  return useContext(ActiveJobContext)
}
