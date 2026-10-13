// The mark of Concilia: two sources (model A in slate blue and model B in
// plum) that meet in one validated line (green). Used in the sidebar and,
// larger, on the home page.
function Logo({ className = 'size-6' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden fill="none" strokeWidth="2.5" strokeLinecap="round">
      <path d="M3 5 C 9 5, 9 12, 13 12" className="stroke-model-a" />
      <path d="M3 19 C 9 19, 9 12, 13 12" className="stroke-model-b" />
      <path d="M13 12 H 21" className="stroke-accent" />
    </svg>
  )
}

export default Logo
