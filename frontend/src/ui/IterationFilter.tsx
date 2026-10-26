// A filter by iteration of the project (step 111): all, or the elements that
// come from the documentation of one of them. count: how many the project has.
function IterationFilter({ count, value, onChange }: { count: number; value: number | null; onChange: (value: number | null) => void }) {
  return (
    <select
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value === '' ? null : Number(e.target.value))}
      aria-label="Filtrar por iteración"
      className="rounded border border-line bg-surface px-2.5 py-1.5 text-sm"
    >
      <option value="">Todas las iteraciones</option>
      {Array.from({ length: count }, (_, i) => i + 1).map((n) => (
        <option key={n} value={n}>
          Iteración {n}
        </option>
      ))}
    </select>
  )
}

export default IterationFilter
