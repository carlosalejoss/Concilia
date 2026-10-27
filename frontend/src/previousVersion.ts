import { useEffect, useState } from 'react'

// Step 115: a catalog or backlog generated from scratch against the version
// the deliverable had just before (GET /jobs/{id}/previous-version). Each
// generated element is the same as one of before, changed or new, by meaning
// (embeddings, as A and B); what was before and is not now has disappeared.
// The ids of the generation stay when the user edits (step 110), so the marks
// follow the elements; one added by hand has none.

export type VersionMark = 'Same' | 'Changed' | 'New'

export type MarkedElement = { id: string; text: string; mark: VersionMark; previousId: string | null; previousText: string | null; similarity: number }
export type GoneElement = { id: string; text: string; bestId: string | null; bestText: string | null; similarity: number }

export type PreviousComparison = {
  version: number
  status: 'Draft' | 'Validated'
  elements: MarkedElement[]
  gone: GoneElement[]
}

// The comparison of the job of the deliverable, or null (first version, an
// update, or the API could not give it).
export function usePreviousVersion(jobId: string | undefined): PreviousComparison | null {
  const [comparison, setComparison] = useState<PreviousComparison | null>(null)
  useEffect(() => {
    if (!jobId) return
    fetch(`/jobs/${jobId}/previous-version`)
      .then(async (response) => setComparison(response.ok ? ((await response.json()).comparison ?? null) : null))
      .catch(() => setComparison(null))
  }, [jobId])
  return comparison
}

export const markOf = (comparison: PreviousComparison | null, id: string) => comparison?.elements.find((e) => e.id === id) ?? null

export const versionText = (c: PreviousComparison) => `la versión ${c.version} (${c.status === 'Validated' ? 'validada' : 'en borrador'})`
