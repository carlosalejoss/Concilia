import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ActiveJobContext, type ActiveJob } from './hooks/useActiveJob.ts'
import Banner from './ui/Banner.tsx'

const CHECK_EVERY_MS = 3000
const WARNING_MS = 5000

// Asks the API every 3 s which task is running (if any) and shares it with
// every button that launches one. When such a button is pressed during a
// task, a notice at the bottom of the page says which task and where.
function ActiveJobProvider({ children }: { children: ReactNode }) {
  const [active, setActive] = useState<ActiveJob | null>(null)
  const [warning, setWarning] = useState<string | null>(null)

  const refresh = useCallback(() => {
    fetch('/jobs/active')
      .then(async (response) => setActive(response.status === 200 ? ((await response.json()) as ActiveJob) : null))
      .catch(() => setActive(null)) // without the API nothing is blocked here; the API refuses anyway
  }, [])

  useEffect(() => {
    refresh()
    const timer = setInterval(refresh, CHECK_EVERY_MS)
    return () => clearInterval(timer)
  }, [refresh])

  // The notice goes away by itself after a few seconds.
  useEffect(() => {
    if (warning === null) return
    const timer = setTimeout(() => setWarning(null), WARNING_MS)
    return () => clearTimeout(timer)
  }, [warning])

  const warn = useCallback(() => setWarning(active?.message ?? null), [active])
  const state = useMemo(() => ({ active, refresh, warn }), [active, refresh, warn])

  return (
    <ActiveJobContext.Provider value={state}>
      {children}
      {warning && (
        <div className="fixed inset-x-0 bottom-6 z-50 flex justify-center px-4">
          <div className="w-full max-w-lg rounded bg-surface shadow-lg">
            <Banner tone="warn">{warning}</Banner>
          </div>
        </div>
      )}
    </ActiveJobContext.Provider>
  )
}

export default ActiveJobProvider
