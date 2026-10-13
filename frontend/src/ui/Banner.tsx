import type { ReactNode } from 'react'
import { AlertTriangle, CircleAlert, Info, CheckCircle2, type LucideIcon } from 'lucide-react'
import { TONES } from './tones.ts'

type BannerTone = 'info' | 'warn' | 'danger' | 'ok' | 'stale'

const ICONS: Record<BannerTone, LucideIcon> = {
  info: Info,
  warn: AlertTriangle,
  danger: CircleAlert,
  ok: CheckCircle2,
  stale: AlertTriangle,
}

// A message about the whole view (why something is blocked, an error, a
// warning), with an optional action on the right.
function Banner({ tone = 'info', children, action }: { tone?: BannerTone; children: ReactNode; action?: ReactNode }) {
  const Icon = ICONS[tone]
  return (
    <div role={tone === 'danger' ? 'alert' : 'status'} className={`flex items-start gap-3 rounded px-3.5 py-2.5 text-sm ${TONES[tone]}`}>
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div className="min-w-0 flex-1">{children}</div>
      {action}
    </div>
  )
}

export default Banner
