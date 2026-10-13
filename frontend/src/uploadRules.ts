// Same rules as the API (src/Concilia.Core/Files/UploadRules.cs); keep both
// in sync. The API always checks them again: this copy only lets the web warn
// before sending the file.

export const MAX_BYTES = 10 * 1024 * 1024 // 10 MB

export const ALLOWED_EXTENSIONS = ['.txt', '.md', '.pdf', '.docx']

// Returns an error message for the user, or null if the file is valid.
export function validateUpload(file: File): string | null {
  const dot = file.name.lastIndexOf('.')
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : ''

  if (!ALLOWED_EXTENSIONS.includes(extension)) {
    return `Tipo de archivo no permitido. Se admiten: ${ALLOWED_EXTENSIONS.join(', ')}.`
  }
  if (file.size === 0) {
    return 'El archivo está vacío.'
  }
  if (file.size > MAX_BYTES) {
    return `El archivo supera el tamaño máximo de ${MAX_BYTES / (1024 * 1024)} MB.`
  }
  return null
}
