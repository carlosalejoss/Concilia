import { useEffect, useState, type DragEvent } from 'react'
import { ChevronRight, FileText, Upload } from 'lucide-react'
import { errorMessage, formatDate, type Project, type ProjectFile } from './api.ts'
import { ALLOWED_EXTENSIONS, MAX_BYTES, validateUpload } from './uploadRules.ts'
import Banner from './ui/Banner.tsx'
import Badge from './ui/Badge.tsx'

type ProjectFilesProps = {
  project: Project
  // Called after a file is uploaded (the state of the steps depends on the files).
  onChange?: () => void
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.')
  return dot >= 0 ? name.slice(dot + 1).toUpperCase() : ''
}

// The documents step: a zone to drop files (or click to choose them) and the
// list of files; each one with text opens to show what was extracted from it.
function ProjectFiles({ project, onChange }: ProjectFilesProps) {
  const [files, setFiles] = useState<ProjectFile[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  // The extracted text of each file opened, by id (loaded when it opens).
  const [texts, setTexts] = useState<Record<number, string>>({})

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
        onChange?.()
      } catch (e: unknown) {
        setErrors((current) => [...current, `${file.name}: ${String(e)}`])
      }
    }
    setUploading(false)
  }

  async function toggleText(file: ProjectFile) {
    if (file.id in texts) {
      setTexts((current) => {
        const rest = { ...current }
        delete rest[file.id]
        return rest
      })
      return
    }
    try {
      const response = await fetch(`/projects/${project.id}/files/${file.id}/text`)
      if (!response.ok) {
        setErrors([`${file.fileName}: ${await errorMessage(response)}`])
        return
      }
      const text = await response.text()
      setTexts((current) => ({ ...current, [file.id]: text }))
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
    <div className="space-y-4">
      {/* The <label> wraps a hidden file input, so clicking the zone also opens
          the file picker. Smaller once there are files. */}
      <label
        onDragOver={(e) => {
          e.preventDefault() // Needed so the browser allows dropping here.
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        className={`flex cursor-pointer items-center gap-3 rounded border border-dashed px-4 transition-colors ${
          files.length === 0 ? 'flex-col py-10 text-center' : 'py-3'
        } ${dragging ? 'border-accent bg-accent-soft' : 'border-line hover:border-faint'}`}
      >
        <Upload className={`shrink-0 text-faint ${files.length === 0 ? 'size-7' : 'size-4'}`} aria-hidden />
        <span className="text-sm">
          <span className="font-medium">{uploading ? 'Subiendo…' : 'Arrastra los archivos aquí o elígelos'}</span>
          <span className="block text-muted">
            {ALLOWED_EXTENSIONS.join(', ')}, hasta {MAX_BYTES / (1024 * 1024)} MB cada uno
          </span>
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
        <Banner key={error} tone="danger">
          {error}
        </Banner>
      ))}

      {files.length > 0 && (
        <ul className="divide-y divide-line rounded border border-line bg-surface">
          {files.map((file) => {
            const open = file.id in texts
            const text = texts[file.id]
            return (
              <li key={file.id}>
                <div className="flex items-center gap-3 px-3 py-2.5">
                  <FileText className="size-4 shrink-0 text-faint" aria-hidden />
                  <span className="min-w-0 flex-1 truncate font-medium" title={`SHA-256: ${file.sha256}`}>
                    {file.fileName}
                  </span>
                  <Badge>{extensionOf(file.fileName)}</Badge>
                  <span className="w-20 text-right text-sm text-muted">{formatSize(file.sizeBytes)}</span>
                  <span className="hidden w-36 text-right text-sm text-muted sm:block">{formatDate(file.uploadedAt)}</span>
                  {file.textStoredName ? (
                    <button
                      type="button"
                      onClick={() => toggleText(file)}
                      aria-expanded={open}
                      className="flex items-center gap-1 rounded px-1.5 py-1 text-sm text-accent hover:bg-accent-soft"
                    >
                      <ChevronRight className={`size-4 transition-transform ${open ? 'rotate-90' : ''}`} aria-hidden />
                      Texto
                    </button>
                  ) : (
                    <span className="w-16 text-right text-xs text-faint">Sin texto</span>
                  )}
                </div>
                {open &&
                  (text.trim() === '' ? (
                    <p className="border-t border-line px-10 py-3 text-sm text-muted">
                      No se ha encontrado texto. Si es un PDF escaneado, sus páginas son imágenes y no se puede extraer texto sin OCR.
                    </p>
                  ) : (
                    // The text as the models read it; whitespace-pre-wrap keeps the line breaks.
                    <pre className="max-h-96 overflow-auto border-t border-line bg-canvas px-10 py-3 font-serif text-sm leading-relaxed whitespace-pre-wrap">
                      {text}
                    </pre>
                  ))}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

export default ProjectFiles
