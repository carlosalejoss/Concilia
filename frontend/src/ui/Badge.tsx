import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { TONES, type Tone } from './tones.ts'

// A small label for a state or a count: always the same colors for the same meaning.
function Badge({ tone = 'neutral', icon: Icon, children }: { tone?: Tone; icon?: LucideIcon; children: ReactNode }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap ${TONES[tone]}`}>
      {Icon && <Icon className="size-3.5" aria-hidden />}
      {children}
    </span>
  )
}

export default Badge
