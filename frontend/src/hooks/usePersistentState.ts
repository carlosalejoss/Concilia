import { useEffect, useState } from 'react'

// A state that the browser remembers (localStorage), such as a section the
// user folded. If the browser does not allow it (private window, blocked
// storage), it works as a normal state: nothing breaks, it is only forgotten.
export function usePersistentState<T>(key: string, initial: T) {
  const [value, setValue] = useState<T>(() => {
    try {
      const saved = localStorage.getItem(key)
      return saved === null ? initial : (JSON.parse(saved) as T)
    } catch {
      return initial
    }
  })

  useEffect(() => {
    try {
      localStorage.setItem(key, JSON.stringify(value))
    } catch {
      // Not remembered; the state still works.
    }
  }, [key, value])

  return [value, setValue] as const
}
