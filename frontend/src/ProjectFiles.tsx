import { useEffect, useState, type DragEvent } from 'react'
import { BookOpen, FileText, Palette, Upload } from 'lucide-react'
import { errorMessage, formatDate, type Project, type ProjectFile } from './api.ts'
import { ALLOWED_EXTENSIONS, MAX_BYTES, isStyleFile, validateUpload } from './uploadRules.ts'
import Banner from './ui/Banner.tsx'
import Badge from './ui/Badge.tsx'
import Drawer from './ui/Drawer.tsx'
import FilePreview from './FilePreview.tsx'

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
  // The file whose extracted text is open in the reader, with its text.
  const [reading, setReading] = useState<{ file: ProjectFile; text: string } | null>(null)

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

  async function openText(file: ProjectFile) {
    try {
      const response = await fetch(`/projects/${project.id}/files/${file.id}/text`)
      if (!response.ok) {
        setErrors([`${file.fileName}: ${await errorMessage(response)}`])
        return
      }
      setReading({ file, text: await response.text() })
    } catch (e: unknown) {
      setErrors([`${file.fileName}: ${String(e)}`])
    }
  }

  // Size of the text as the models see it: words, lines and estimated tokens
  // (3.5 characters per token, as the platform estimates it).
  function textStats(text: string): string {
    const words = text.split(/\s+/).filter(Boolean).length
    const lines = text.split(/\r?\n/).filter((l) => l.trim() !== '').length
    return `${words.toLocaleString('es-ES')} palabras, ${lines.toLocaleString('es-ES')} líneas, unos ${Math.ceil(text.length / 3.5).toLocaleString('es-ES')} tokens`
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
            {ALLOWED_EXTENSIONS.filter((e) => !isStyleFile(e)).join(' ')}, hasta {MAX_BYTES / (1024 * 1024)} MB cada uno
          </span>
          <span className="block text-muted">.zip con un mockup del cliente: se usa como referencia de estilo del mockup</span>
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
            const style = isStyleFile(file.fileName)
            const Icon = style ? Palette : FileText
            return (
              <li key={file.id}>
                <div className="flex items-center gap-3 px-3 py-2.5">
                  <Icon className="size-4 shrink-0 text-faint" aria-hidden />
                  <span className="min-w-0 flex-1 truncate font-medium" title={`SHA-256: ${file.sha256}`}>
                    {file.fileName}
                  </span>
                  {style && (
                    <span title="No es documentación: solo lo usa el mockup, que copia su estilo (colores, fuentes, componentes e imágenes).">
                      <Badge tone="info">Estilo</Badge>
                    </span>
                  )}
                  <Badge>{extensionOf(file.fileName)}</Badge>
                  <span className="w-20 text-right text-sm text-muted">{formatSize(file.sizeBytes)}</span>
                  <span className="hidden w-36 text-right text-sm text-muted sm:block">{formatDate(file.uploadedAt)}</span>
                  {file.textStoredName ? (
                    <button
                      type="button"
                      onClick={() => openText(file)}
                      className="flex items-center gap-1.5 rounded px-2 py-1 text-sm text-accent hover:bg-accent-soft"
                    >
                      <BookOpen className="size-4" aria-hidden />
                      Leer
                    </button>
                  ) : (
                    <span className="w-16 text-right text-xs text-faint">{style ? 'Mockup' : 'Sin texto'}</span>
                  )}
                </div>
              </li>
            )
          })}
        </ul>
      )}

      {/* The extracted text, the one the models read, in a wide reader. */}
      <Drawer
        open={reading !== null}
        wide
        title={reading?.file.fileName ?? ''}
        subtitle={reading && reading.text.trim() !== '' ? `Texto extraído: ${textStats(reading.text)}` : undefined}
        onClose={() => setReading(null)}
      >
        {reading &&
          (reading.text.trim() === '' ? (
            <p className="text-sm text-muted">
              No se ha encontrado texto. Si es un PDF escaneado, sus páginas son imágenes y no se puede extraer texto sin OCR.
            </p>
          ) : (
            <FilePreview fileName={reading.file.fileName} text={reading.text} />
          ))}
      </Drawer>
    </div>
  )
}

export default ProjectFiles
