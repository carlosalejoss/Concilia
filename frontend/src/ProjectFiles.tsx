import { useEffect, useState, type DragEvent } from 'react'
import { errorMessage, formatDate, type Project, type ProjectFile } from './api.ts'
import { ALLOWED_EXTENSIONS, MAX_BYTES, validateUpload } from './uploadRules.ts'

type ProjectFilesProps = {
  project: Project
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

// Detail of one project: a drop zone to upload files and the list of files.
// App.tsx gives this component a new "key" for each project, so React creates
// a fresh one (with empty state) whenever another project is selected.
function ProjectFiles({ project }: ProjectFilesProps) {
  const [files, setFiles] = useState<ProjectFile[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [shown, setShown] = useState<{ file: ProjectFile; text: string } | null>(null)

  useEffect(() => {
    fetch(`/projects/${project.id}/files`)
      .then(async (response) => {
        if (!response.ok) {
          setErrors([await errorMessage(response)])
          return
        }
        setFiles(await response.json())
      })
      .catch((e: unknown) => setErrors([String(e)]))
  }, [project.id])

  // Uploads the files one by one, so each can succeed or fail on its own.
  async function upload(selected: File[]) {
    setUploading(true)
    setErrors([])
    for (const file of selected) {
      const invalid = validateUpload(file)
      if (invalid) {
        setErrors((current) => [...current, `${file.name}: ${invalid}`])
        continue
      }
      try {
        const form = new FormData()
        form.append('file', file) // Field name expected by the API.
        const response = await fetch(`/projects/${project.id}/files`, { method: 'POST', body: form })
        if (!response.ok) {
          const message = await errorMessage(response)
          setErrors((current) => [...current, `${file.name}: ${message}`])
          continue
        }
        const created: ProjectFile = await response.json()
        setFiles((current) => [created, ...current])
      } catch (e: unknown) {
        setErrors((current) => [...current, `${file.name}: ${String(e)}`])
      }
    }
    setUploading(false)
  }

  async function showText(file: ProjectFile) {
    setErrors([])
    try {
      const response = await fetch(`/projects/${project.id}/files/${file.id}/text`)
      if (!response.ok) {
        setErrors([`${file.fileName}: ${await errorMessage(response)}`])
        return
      }
      setShown({ file, text: await response.text() })
    } catch (e: unknown) {
      setErrors([`${file.fileName}: ${String(e)}`])
    }
  }

  function handleDrop(event: DragEvent) {
    // Without preventDefault the browser would open the dropped file itself.
    event.preventDefault()
    setDragging(false)
    upload(Array.from(event.dataTransfer.files))
  }

  return (
    <section className="mt-8">
      <h2 className="text-xl font-semibold">Archivos de «{project.name}»</h2>

      {/* The <label> wraps a hidden file input, so clicking the zone also opens
          the file picker. */}
      <label
        onDragOver={(e) => {
          e.preventDefault() // Needed so the browser allows dropping here.
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        className={`mt-4 block cursor-pointer rounded border-2 border-dashed p-8 text-center ${
          dragging ? 'border-blue-600 bg-blue-50' : 'border-gray-300'
        }`}
      >
        {uploading ? 'Subiendo…' : 'Arrastra archivos aquí o haz clic para elegirlos'}
        <span className="mt-1 block text-sm text-gray-500">
          {ALLOWED_EXTENSIONS.join(', ')} · máximo {MAX_BYTES / (1024 * 1024)} MB por archivo
        </span>
        <input
          type="file"
          multiple
          accept={ALLOWED_EXTENSIONS.join(',')}
          className="hidden"
          disabled={uploading}
          onChange={(e) => {
            upload(Array.from(e.target.files ?? []))
            e.target.value = '' // Allow choosing the same file again.
          }}
        />
      </label>

      {errors.map((error) => (
        <p key={error} className="mt-2 text-red-700">
          {error}
        </p>
      ))}

      {files.length === 0 ? (
        <p className="mt-4 text-gray-500">Este proyecto todavía no tiene archivos.</p>
      ) : (
        <table className="mt-4 w-full text-left text-sm">
          <thead className="text-gray-500">
            <tr>
              <th className="py-1">Nombre</th>
              <th className="py-1">Tipo</th>
              <th className="py-1 text-right">Tamaño</th>
              <th className="py-1 text-right">Subido</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {files.map((file) => (
              <tr key={file.id}>
                {/* The SHA-256 hash is shown as a tooltip on hover. Files with
                    extracted text can be clicked to show it. */}
                <td className="py-1" title={`SHA-256: ${file.sha256}`}>
                  {file.textStoredName ? (
                    <button
                      type="button"
                      onClick={() => showText(file)}
                      className={`text-left text-blue-700 underline ${
                        shown?.file.id === file.id ? 'font-semibold' : ''
                      }`}
                    >
                      {file.fileName}
                    </button>
                  ) : (
                    file.fileName
                  )}
                </td>
                <td className="py-1 text-gray-500">{file.contentType}</td>
                <td className="py-1 text-right">{formatSize(file.sizeBytes)}</td>
                <td className="py-1 text-right text-gray-500">{formatDate(file.uploadedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {shown && (
        <div className="mt-6">
          <h3 className="font-semibold">Texto extraído de «{shown.file.fileName}»</h3>
          {shown.text.trim() === '' ? (
            <p className="mt-2 text-gray-500">
              No se ha encontrado texto. Si es un PDF escaneado, sus páginas son imágenes y no se
              puede extraer texto sin OCR.
            </p>
          ) : (
            // <pre> keeps the line breaks; whitespace-pre-wrap wraps long lines.
            <pre className="mt-2 max-h-96 overflow-auto whitespace-pre-wrap rounded border border-gray-200 bg-gray-50 p-3 text-sm">
              {shown.text}
            </pre>
          )}
        </div>
      )}
    </section>
  )
}

export default ProjectFiles
