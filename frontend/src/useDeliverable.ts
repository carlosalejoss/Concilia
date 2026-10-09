import { useCallback, useEffect, useState } from 'react'
import { errorMessages, type Deliverable } from './api.ts'

// What the catalog and backlog editors share: loading the deliverable, saving
// the user's changes, validating and reopening it. kind: "catalog" or "backlog".
export function useDeliverable<T>(projectId: number, kind: string, version: number, onChange: () => void) {
  const [deliverable, setDeliverable] = useState<Deliverable<T> | null>(null)
  const [draft, setDraft] = useState<T | null>(null)
  const [dirty, setDirty] = useState(false)
  const [busy, setBusy] = useState(false)
  const [messages, setMessages] = useState<string[]>([])

  const url = `/projects/${projectId}/deliverables/${kind}`

  const load = useCallback(() => {
    fetch(url)
      .then(async (response) => (response.ok ? ((await response.json()) as Deliverable<T>) : null))
      .then((d) => {
        setDeliverable(d)
        setDraft(d?.content ?? null)
        setDirty(false)
      })
      .catch((e: unknown) => setMessages([String(e)]))
  }, [url])

  useEffect(() => {
    load()
  }, [load, version])

  function edit(next: T) {
    setDraft(next)
    setDirty(true)
  }

  // Sends one request; on success the new state replaces the old one.
  async function send(path: string, method: string, body?: unknown): Promise<boolean> {
    const response = await fetch(url + path, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
    if (!response.ok) {
      setMessages(await errorMessages(response))
      return false
    }
    const d: Deliverable<T> = await response.json()
    setDeliverable(d)
    setDraft(d.content)
    setDirty(false)
    return true
  }

  async function run(action: () => Promise<boolean>) {
    setBusy(true)
    setMessages([])
    try {
      if (await action()) onChange()
    } catch (e: unknown) {
      setMessages([String(e)])
    } finally {
      setBusy(false)
    }
  }

  const save = () => run(() => send('', 'PUT', { content: draft }))
  // Unsaved changes are saved first, so what is validated is what the user sees.
  const validate = () =>
    run(async () => (dirty ? await send('', 'PUT', { content: draft }) : true) && (await send('/validate', 'POST')))
  const reopen = () => run(() => send('/reopen', 'POST'))
  const discard = () => {
    setDraft(deliverable?.content ?? null)
    setDirty(false)
  }

  return { deliverable, draft, dirty, busy, messages, edit, save, validate, reopen, discard }
}
