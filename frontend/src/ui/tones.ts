// The colors of each tone (soft background and text), shared by Badge and Banner.
export type Tone = 'neutral' | 'accent' | 'ok' | 'warn' | 'danger' | 'info' | 'stale'

export const TONES: Record<Tone, string> = {
  neutral: 'bg-sunken text-muted',
  accent: 'bg-accent-soft text-accent',
  ok: 'bg-ok-soft text-ok',
  warn: 'bg-warn-soft text-warn',
  danger: 'bg-danger-soft text-danger',
  info: 'bg-info-soft text-info',
  stale: 'bg-stale-soft text-stale',
}
