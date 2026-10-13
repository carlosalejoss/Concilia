import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

// What a view shows when it has nothing yet: an icon, what is missing and,
// if there is one, the action that fills it.
function EmptyState({ icon: Icon, title, children, action }: { icon: LucideIcon; title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center rounded-lg border border-dashed border-line px-6 py-12 text-center">
      <Icon className="size-8 text-faint" aria-hidden />
      <p className="mt-3 font-medium">{title}</p>
      {children && <p className="mt-1 max-w-md text-sm text-muted">{children}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

export default EmptyState
