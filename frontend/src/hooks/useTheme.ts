import { useEffect } from 'react'
import { usePersistentState } from './usePersistentState.ts'

export type Theme = 'light' | 'dark'

// The theme the user chose, or the system's the first time. index.html sets
// the class before React starts, so the page does not flash light first.
export function useTheme() {
  const [theme, setTheme] = usePersistentState<Theme>(
    'concilia.theme',
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light',
  )

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark')
  }, [theme])

  const toggle = () => setTheme(theme === 'dark' ? 'light' : 'dark')
  return [theme, toggle] as const
}
