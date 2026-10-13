import { useEffect, type ReactNode } from 'react'
import { X } from 'lucide-react'
import Button from './Button.tsx'

// A panel that slides in from the right over the page (the project settings,
// the text of a document), closed with its button, Escape or a click outside.
// wide: for reading (the text of a document); subtitle: a line under the title.
function Drawer({
  open,
  title,
  subtitle,
  wide = false,
  onClose,
  children,
}: {
  open: boolean
  title: string
  subtitle?: ReactNode
  wide?: boolean
  onClose: () => void
  children: ReactNode
}) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="fixed inset-0 z-40 flex justify-end">
      <button type="button" aria-label="Cerrar" onClick={onClose} className="absolute inset-0 bg-black/30" />
      <aside role="dialog" aria-label={title} className={`relative flex h-full w-full flex-col border-l border-line bg-surface shadow-xl ${wide ? 'max-w-3xl' : 'max-w-md'}`}>
        <header className="flex items-center justify-between gap-3 border-b border-line px-5 py-3">
          <div className="min-w-0">
            <h2 className="truncate font-semibold">{title}</h2>
            {subtitle && <p className="text-xs text-muted">{subtitle}</p>}
          </div>
          <Button variant="ghost" size="sm" icon={X} onClick={onClose} aria-label="Cerrar" />
        </header>
        <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>
      </aside>
    </div>
  )
}

export default Drawer
