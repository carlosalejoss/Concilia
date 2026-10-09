import type { useDeliverable } from './useDeliverable.ts'

// The bar of a deliverable: its state, the buttons and what keeps it from
// being validated (or why an action failed).
function DeliverableBar<T>({
  state,
  name,
}: {
  state: ReturnType<typeof useDeliverable<T>>
  name: string
}) {
  const { deliverable: d, dirty, busy, messages, save, validate, reopen, discard } = state
  if (!d) return null
  const validated = d.status === 'Validated'
  const shown = messages.length > 0 ? messages : validated ? [] : d.errors

  return (
    <div className="mt-3 space-y-2 text-sm">
      <div className="flex flex-wrap items-center gap-2">
        {validated ? (
          <>
            <span className="font-semibold text-green-700">
              {name} validado{d.validatedAt && ` el ${new Date(d.validatedAt).toLocaleString('es-ES')}`}: solo lectura.
            </span>
            <button type="button" disabled={busy} onClick={reopen} className="rounded border border-gray-400 px-3 py-1 disabled:opacity-50">
              Reabrir
            </button>
          </>
        ) : (
          <>
            <button type="button" disabled={busy || !dirty} onClick={save} className="rounded border border-gray-400 px-3 py-1 disabled:opacity-50">
              Guardar cambios
            </button>
            <button type="button" disabled={busy || !dirty} onClick={discard} className="rounded border border-gray-400 px-3 py-1 disabled:opacity-50">
              Deshacer cambios
            </button>
            <button type="button" disabled={busy} onClick={validate} className="rounded bg-green-700 px-3 py-1 text-white disabled:opacity-50">
              Validar
            </button>
            {dirty && <span className="text-amber-700">Hay cambios sin guardar.</span>}
          </>
        )}
      </div>
      {shown.length > 0 && (
        <div className="rounded border border-red-200 bg-red-50 p-2 text-red-800">
          <p className="font-semibold">{messages.length > 0 ? 'No se ha podido:' : 'Antes de validar hay que corregir:'}</p>
          <ul className="list-disc pl-5">
            {shown.map((m, i) => (
              <li key={i}>{m}</li>
            ))}
          </ul>
        </div>
      )}
      {!validated && d.warnings.length > 0 && (
        <div className="rounded border border-amber-200 bg-amber-50 p-2 text-amber-800">
          <p className="font-semibold">Avisos (no impiden validar):</p>
          <ul className="list-disc pl-5">
            {d.warnings.map((m, i) => (
              <li key={i}>{m}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

export default DeliverableBar
