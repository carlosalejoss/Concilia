import { useEffect, useState } from 'react'
import type { ElementIterations } from './api.ts'

// The iteration of each element (step 111). The platform gives that of each
// document and question (GET /projects/{id}/element-iterations); with them,
// a requirement is of those of its sources and questions, and a story of those
// of its requirements, as ElementIterations computes it in the API. It is
// computed here too so that it follows what the user edits before saving.

// version: when it changes (the page reloaded the flow), the map is read again.
export function useElementIterations(projectId: number, version: number): ElementIterations | null {
  const [map, setMap] = useState<ElementIterations | null>(null)
  useEffect(() => {
    fetch(`/projects/${projectId}/element-iterations`)
      .then(async (response) => (response.ok ? setMap(await response.json()) : setMap(null)))
      .catch(() => setMap(null))
  }, [projectId, version])
  return map
}

const sorted = (numbers: Iterable<number>) => [...new Set(numbers)].sort((a, b) => a - b)

// The iterations of what cites these fragments and questions: a question (its
// sources) or a requirement (its sources and its questions).
export function iterationsOf(map: ElementIterations, sources: string[], questions: string[] = []): number[] {
  const numbers: number[] = []
  for (const fragment of sources) {
    const number = map.documents[fragment.split('#')[0]]
    if (number !== undefined) numbers.push(number)
  }
  for (const question of questions) numbers.push(...(map.questions[question] ?? []))
  return sorted(numbers)
}

// The iterations of a story: those of its requirements (by id).
export function storyIterations(requirements: Map<string, number[]>, ids: string[]): number[] {
  return sorted(ids.flatMap((id) => requirements.get(id) ?? []))
}

// Whether to show the iterations at all: only once the project has more than
// one, so a project of one iteration looks as before.
export const showIterations = (map: ElementIterations | null): map is ElementIterations => map !== null && map.count > 1

// "la iteración 2", "las iteraciones 1 y 2", "las iteraciones 1, 2 y 3".
export function iterationsText(numbers: number[]): string {
  if (numbers.length === 1) return `la iteración ${numbers[0]}`
  return `las iteraciones ${numbers.slice(0, -1).join(', ')} y ${numbers[numbers.length - 1]}`
}
