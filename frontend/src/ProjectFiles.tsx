import { useCallback, useEffect, useState, type DragEvent } from 'react'
import { BookOpen, FileText, Lock, LockOpen, Palette, Pencil, Plus, Trash2, Upload } from 'lucide-react'
import { errorMessage, formatDate, type Iteration, type Project, type ProjectFile } from './api.ts'
import { ALLOWED_EXTENSIONS, MAX_BYTES, isStyleFile, validateUpload } from './uploadRules.ts'
import Banner from './ui/Banner.tsx'
import Badge from './ui/Badge.tsx'
import Button from './ui/Button.tsx'
import Collapsible from './ui/Collapsible.tsx'
import Drawer from './ui/Drawer.tsx'
import FilePreview from './FilePreview.tsx'

type ProjectFilesProps = {
  project: Project
  // Called after something that changes the state of the steps: a file
  // uploaded, an iteration closed, reopened, created or deleted.
  onChange?: () => void
}

const field = 'w-full rounded-md border border-line bg-surface px-3 py-2 text-sm placeholder:text-faint focus:outline-2 focus:outline-accent'

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
// files grouped by iteration of the project (step 109), the newest first.
// Documents go to the open iteration; "Cerrar documentación" closes it, and
// until then the phases that read the documentation wait. The style
// reference of the mockup (.zip) belongs to the project and goes apart.
// Each file with text opens to show what was extracted from it.
function ProjectFiles({ project, onChange }: ProjectFilesProps) {
  const [files, setFiles] = useState<ProjectFile[]>([])
  const [iterations, setIterations] = useState<Iteration[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [uploading, setUploading] = useState(false)
  const [busy, setBusy] = useState(false)
  const [dragging, setDragging] = useState(false)
  // The iteration whose name and notes are being edited, with what is typed.
  const [editing, setEditing] = useState<{ number: number; name: string; notes: string } | null>(null)
  // The iteration about to be reopened, waiting for the confirmation.
  const [confirmingReopen, setConfirmingReopen] = useState<number | null>(null)
  // The file whose extracted text is open in the reader, with its text.
  const [reading, setReading] = useState<{ file: ProjectFile; text: string } | null>(null)

  const load = useCallback(() => {
    Promise.all([fetch(`/projects/${project.id}/files`), fetch(`/projects/${project.id}/iterations`)])
      .then(async ([filesResponse, iterationsResponse]) => {
        if (!filesResponse.ok || !iterationsResponse.ok) {
          setErrors([await errorMessage(filesResponse.ok ? iterationsResponse : filesResponse)])
          return
        }
        setFiles(await filesResponse.json())
        setIterations(await iterationsResponse.json())
      })
      .catch((e: unknown) => setErrors([String(e)]))
  }, [project.id])

  useEffect(() => {
    load()
  }, [load])

  const open = iterations.find((i) => i.status === 'Open') ?? null
  const last = iterations.length > 0 ? Math.max(...iterations.map((i) => i.number)) : 0
  const styleFiles = files.filter((f) => isStyleFile(f.fileName))

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

  // An action on the iterations (create, edit, close, reopen, delete): on
  // success, the files and iterations are read again and the page reloads the flow.
  async function act(url: string, method: string, body?: unknown): Promise<boolean> {
    setBusy(true)
    setErrors([])
    try {
      const response = await fetch(url, {
        method,
        headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
      })
      if (!response.ok) {
        setErrors([await errorMessage(response)])
        return false
      }
      load()
      onChange?.()
      return true
    } catch (e: unknown) {
      setErrors([String(e)])
      return false
    } finally {
      setBusy(false)
    }
  }

  async function saveEdit() {
    if (!editing) return
    const done = await act(`/projects/${project.id}/iterations/${editing.number}`, 'PUT', { name: editing.name, notes: editing.notes })
    if (done) setEditing(null)
  }

  async function reopen(number: number) {
    const done = await act(`/projects/${project.id}/iterations/${number}/reopen`, 'POST')
    if (done) setConfirmingReopen(null)
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

  function fileRow(file: ProjectFile) {
    const style = isStyleFile(file.fileName)
    const Icon = style ? Palette : FileText
    return (
      <li key={file.id}>
        <div className="flex items-center gap-3 py-2.5">
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
            {open
              ? `Los documentos se suben a «${open.name}»: ${ALLOWED_EXTENSIONS.filter((e) => !isStyleFile(e)).join(' ')}, hasta ${MAX_BYTES / (1024 * 1024)} MB cada uno`
              : 'La documentación está cerrada: para añadir documentos, crea una iteración nueva'}
          </span>
          <span className="block text-muted">.zip con un mockup del cliente: se usa como referencia de estilo del mockup y se puede subir siempre</span>
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

      {iterations.length > 0 && !open && (
        <div className="flex justify-end">
          <Button icon={Plus} disabled={busy} onClick={() => act(`/projects/${project.id}/iterations`, 'POST', { name: null })}>
            Nueva iteración
          </Button>
        </div>
      )}

      <div className="space-y-3">
        {[...iterations]
          .sort((a, b) => b.number - a.number)
          .map((iteration) => {
            const ofIteration = files.filter((f) => f.iterationId === iteration.id)
            const isOpen = iteration.status === 'Open'
            return (
              <Collapsible
                key={iteration.id}
                persistKey={`p${project.id}.iteracion.${iteration.number}`}
                defaultOpen={iteration.number === last}
                title={iteration.name}
                meta={
                  <>
                    {isOpen ? <Badge tone="info">Abierta</Badge> : <Badge>Cerrada</Badge>}
                    <span className="text-sm text-muted">
                      {ofIteration.length === 1 ? '1 documento' : `${ofIteration.length} documentos`}
                      {iteration.closedAt && `, cerrada el ${formatDate(iteration.closedAt)}`}
                    </span>
                  </>
                }
                actions={
                  <>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={Pencil}
                      disabled={busy}
                      aria-label="Editar el nombre y las notas"
                      title="Editar el nombre y las notas"
                      onClick={() => setEditing({ number: iteration.number, name: iteration.name, notes: iteration.notes ?? '' })}
                    />
                    {isOpen && iteration.number > 1 && ofIteration.length === 0 && (
                      <Button
                        variant="ghost"
                        size="sm"
                        icon={Trash2}
                        disabled={busy}
                        title="Elimina esta iteración, creada por error: está vacía"
                        onClick={() => act(`/projects/${project.id}/iterations/${iteration.number}`, 'DELETE')}
                      >
                        Eliminar
                      </Button>
                    )}
                    {isOpen && (
                      <Button
                        variant="primary"
                        size="sm"
                        icon={Lock}
                        disabled={busy || ofIteration.length === 0}
                        title={
                          ofIteration.length === 0
                            ? 'Sube al menos un documento antes de cerrar la documentación'
                            : 'Termina de subir documentos a esta iteración: después ya se pueden lanzar las preguntas, el catálogo y el backlog'
                        }
                        onClick={() => act(`/projects/${project.id}/iterations/${iteration.number}/close`, 'POST')}
                      >
                        Cerrar documentación
                      </Button>
                    )}
                    {!isOpen && iteration.number === last && (
                      <Button variant="ghost" size="sm" icon={LockOpen} disabled={busy} onClick={() => setConfirmingReopen(iteration.number)}>
                        Reabrir
                      </Button>
                    )}
                  </>
                }
              >
                <div className="space-y-3">
                  {confirmingReopen === iteration.number && (
                    <Banner
                      tone="warn"
                      action={
                        <span className="flex gap-1">
                          <Button variant="ghost" size="sm" disabled={busy} onClick={() => setConfirmingReopen(null)}>
                            Cancelar
                          </Button>
                          <Button variant="primary" size="sm" disabled={busy} onClick={() => reopen(iteration.number)}>
                            Reabrir
                          </Button>
                        </span>
                      }
                    >
                      Podrás subirle más documentos. Mientras esté abierta no se pueden lanzar las preguntas, las sugerencias, el catálogo
                      ni el backlog, y el catálogo, el backlog y el mockup quedan desactualizados, como al subir un archivo.
                    </Banner>
                  )}
                  {editing?.number === iteration.number ? (
                    <div className="space-y-2">
                      <input
                        aria-label="Nombre de la iteración"
                        value={editing.name}
                        onChange={(e) => setEditing({ ...editing, name: e.target.value })}
                        className={field}
                      />
                      <textarea
                        aria-label="Notas de la iteración"
                        placeholder="Notas (opcional): de dónde sale esta documentación, por ejemplo «Reunión del 15-11»"
                        value={editing.notes}
                        onChange={(e) => setEditing({ ...editing, notes: e.target.value })}
                        rows={2}
                        className={field}
                      />
                      <div className="flex gap-2">
                        <Button variant="primary" size="sm" disabled={busy} onClick={saveEdit}>
                          Guardar
                        </Button>
                        <Button variant="ghost" size="sm" disabled={busy} onClick={() => setEditing(null)}>
                          Cancelar
                        </Button>
                      </div>
                    </div>
                  ) : (
                    iteration.notes && <p className="text-sm whitespace-pre-line text-muted">{iteration.notes}</p>
                  )}
                  {ofIteration.length === 0 ? (
                    <p className="text-sm text-muted">Todavía no hay documentos en esta iteración.</p>
                  ) : (
                    <ul className="divide-y divide-line">{ofIteration.map(fileRow)}</ul>
                  )}
                </div>
              </Collapsible>
            )
          })}

        {styleFiles.length > 0 && (
          <Collapsible
            persistKey={`p${project.id}.estilo`}
            title="Referencia de estilo del mockup"
            meta={<span className="text-sm text-muted">del proyecto, no de una iteración</span>}
          >
            <ul className="divide-y divide-line">{styleFiles.map(fileRow)}</ul>
          </Collapsible>
        )}
      </div>

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
