import type { ReactNode } from 'react'
import { ChevronRight } from 'lucide-react'
import { usePersistentState } from '../hooks/usePersistentState.ts'

type CollapsibleProps = {
  // Where the browser remembers whether it is open (e.g. "p27.catalogo.RF").
  persistKey: string
  title: ReactNode
  // Next to the title: counts, states (Badge).
  meta?: ReactNode
  // On the right of the header: actions that do not open or close it.
  actions?: ReactNode
  defaultOpen?: boolean
  children: ReactNode
}

// A section that folds: a header that opens and closes it, and its content.
// Each one remembers its state, so the view is the same when the user comes back.
function Collapsible({ persistKey, title, meta, actions, defaultOpen = false, children }: CollapsibleProps) {
  const [open, setOpen] = usePersistentState(`reqarbiter.open.${persistKey}`, defaultOpen)

  return (
    <section className="rounded border border-line bg-surface">
      <div className="flex items-center gap-2 px-3 py-2">
        <button
          type="button"
          onClick={() => setOpen(!open)}
          aria-expanded={open}
          className="flex min-w-0 flex-1 items-center gap-2 rounded text-left focus-visible:outline-2 focus-visible:outline-accent"
        >
          <ChevronRight className={`size-4 shrink-0 text-faint transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
          <span className="truncate font-medium">{title}</span>
          {meta && <span className="flex shrink-0 items-center gap-1.5">{meta}</span>}
        </button>
        {actions && <div className="flex shrink-0 items-center gap-1.5">{actions}</div>}
      </div>
      {open && <div className="border-t border-line px-3 py-3">{children}</div>}
    </section>
  )
}

export default Collapsible
