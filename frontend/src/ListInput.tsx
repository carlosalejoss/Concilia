// A list typed by the user as "DOC-01#F002, DOC-01#F004" -> ["DOC-01#F002", "DOC-01#F004"].
function splitList(text: string): string[] {
  return text
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

// A comma-separated list edited as text; it becomes a list when the field
// loses focus (the key makes it show the new value after a save).
function ListInput({
  values,
  onChange,
  disabled,
  className,
  placeholder,
}: {
  values: string[]
  onChange: (values: string[]) => void
  disabled: boolean
  className: string
  placeholder?: string
}) {
  return (
    <input
      disabled={disabled}
      defaultValue={values.join(', ')}
      key={values.join(',')}
      placeholder={placeholder}
      onBlur={(e) => {
        const next = splitList(e.target.value)
        if (next.join(',') !== values.join(',')) onChange(next)
      }}
      className={className}
    />
  )
}

export default ListInput
