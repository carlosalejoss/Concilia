import { useActiveJob } from '../hooks/useActiveJob.ts'
import Button, { type ButtonProps } from './Button.tsx'

// A button that launches a task (a round, the catalog, the backlog, the
// mockup, AI suggestions…). While any task runs, in this project or in
// another, it looks switched off and, if pressed, says which task is running
// and where, instead of launching anything (one task at a time, 15-10-2026).
// It is not really disabled, because a disabled button cannot be pressed to
// show the notice; aria-disabled tells screen readers it does nothing.
function LaunchButton({ onClick, className = '', ...rest }: ButtonProps) {
  const { active, warn } = useActiveJob()
  if (active === null) return <Button onClick={onClick} className={className} {...rest} />
  return (
    <Button
      {...rest}
      disabled={false}
      aria-disabled
      title={active.message}
      onClick={warn}
      className={`cursor-not-allowed opacity-50 saturate-50 ${className}`}
    />
  )
}

export default LaunchButton
