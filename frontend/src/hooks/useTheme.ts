import { useEffect } from 'react'
import { flushSync } from 'react-dom'
import { usePersistentState } from './usePersistentState.ts'

export type Theme = 'light' | 'dark'

// Where the change of theme starts: the centre of the button that was pressed.
export type ThemeOrigin = { x: number; y: number }

const REVEAL_MS = 450

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

  // The new theme appears as a circle that grows from the button until it
  // covers the page (16-10-2026). The browser's View Transitions API takes a
  // picture of the page before and after the change; the new one is clipped
  // to a circle that grows. Without that API, or when the user asked the
  // system to reduce motion, the change is instant, as before.
  const toggle = (origin?: ThemeOrigin) => {
    const next: Theme = theme === 'dark' ? 'light' : 'dark'
    const apply = () => {
      document.documentElement.classList.toggle('dark', next === 'dark')
      flushSync(() => setTheme(next)) // the button shows its new icon in the "after" picture too
    }
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    if (!origin || reduceMotion || !document.startViewTransition) {
      apply()
      return
    }

    const transition = document.startViewTransition(apply)
    // The radius that reaches the farthest corner of the window from the button.
    const radius = Math.hypot(Math.max(origin.x, innerWidth - origin.x), Math.max(origin.y, innerHeight - origin.y))
    transition.ready
      .then(() =>
        document.documentElement.animate(
          { clipPath: [`circle(0px at ${origin.x}px ${origin.y}px)`, `circle(${radius}px at ${origin.x}px ${origin.y}px)`] },
          { duration: REVEAL_MS, easing: 'cubic-bezier(0.4, 0, 0.2, 1)', pseudoElement: '::view-transition-new(root)' },
        ),
      )
      .catch(() => {}) // a transition the browser skipped: the theme has changed anyway
  }

  return [theme, toggle] as const
}
