import { CheckCircle2, Lock, RotateCcw, Save, ShieldCheck, Undo2 } from 'lucide-react'
import type { useDeliverable } from './useDeliverable.ts'
import Banner from './ui/Banner.tsx'
import Button from './ui/Button.tsx'
import { formatDate } from './api.ts'

// The bar of a catalog or backlog, fixed at the top while scrolling: its
// state, the buttons to save, undo, validate or reopen it, and below it what
// keeps it from being validated (or why an action failed) and the warnings.
function DeliverableBar<T>({ state, name, summary }: { state: ReturnType<typeof useDeliverable<T>>; name: string; summary: string }) {
  const { deliverable: d, dirty, busy, messages, save, validate, reopen, discard } = state
  if (!d) return null
  const validated = d.status === 'Validated'
  const shown = messages.length > 0 ? messages : validated ? [] : d.errors

  return (
    <div className="sticky top-0 z-10 -mx-8 space-y-2 border-b border-line bg-canvas/95 px-8 py-3 backdrop-blur">
      <div className="flex flex-wrap items-center gap-2">
        <span className="mr-auto flex items-center gap-2 text-sm">
          {validated ? (
            <>
              <Lock className="size-4 text-ok" aria-hidden />
              <span>
                <span className="font-medium text-ok">{name} validado</span>
                {d.validatedAt && <span className="text-muted"> el {formatDate(d.validatedAt)}. Solo lectura.</span>}
              </span>
            </>
          ) : (
            <>
              <span className="text-muted">{summary}</span>
              {dirty && <span className="font-medium text-warn">Cambios sin guardar</span>}
            </>
          )}
        </span>
        {validated ? (
          <Button icon={RotateCcw} disabled={busy} onClick={reopen}>
            Reabrir
          </Button>
        ) : (
          <>
            <Button variant="ghost" icon={Undo2} disabled={busy || !dirty} onClick={discard}>
              Deshacer
            </Button>
            <Button icon={Save} disabled={busy || !dirty} onClick={save}>
              Guardar cambios
            </Button>
            <Button variant="primary" icon={ShieldCheck} disabled={busy} onClick={validate}>
              Validar el {name.toLowerCase()}
            </Button>
          </>
        )}
      </div>
      {shown.length > 0 && (
        <Banner tone="danger">
          <p className="font-medium">{messages.length > 0 ? 'No se ha podido hacer:' : 'Antes de validar hay que corregir:'}</p>
          <ul className="mt-1 max-h-32 list-disc overflow-y-auto pl-5">
            {shown.map((m, i) => (
              <li key={i}>{m}</li>
            ))}
          </ul>
        </Banner>
      )}
      {!validated && d.warnings.length > 0 && (
        <Banner tone="warn">
          <p className="font-medium">Avisos que no impiden validar:</p>
          <ul className="mt-1 max-h-32 list-disc overflow-y-auto pl-5">
            {d.warnings.map((m, i) => (
              <li key={i}>{m}</li>
            ))}
          </ul>
        </Banner>
      )}
      {!validated && shown.length === 0 && d.warnings.length === 0 && !dirty && (
        <p className="flex items-center gap-1.5 text-xs text-ok">
          <CheckCircle2 className="size-3.5" aria-hidden />
          Listo para validar.
        </p>
      )}
    </div>
  )
}

export default DeliverableBar
