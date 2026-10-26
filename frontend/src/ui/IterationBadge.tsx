import { iterationsText } from '../iterations.ts'
import Badge from './Badge.tsx'

// The iterations of the project an element comes from (step 111): "It. 2" or
// "It. 1, 2". Nothing if it has none (an element without sources).
// Coverage (a deliverable or a version): what it includes, said as such.
function IterationBadge({ numbers, coverage = false }: { numbers: number[]; coverage?: boolean }) {
  if (numbers.length === 0) return null
  return (
    <span title={coverage ? `Incluye la documentación de ${iterationsText(numbers)}` : `Sale de la documentación de ${iterationsText(numbers)}`}>
      <Badge>It. {numbers.join(', ')}</Badge>
    </span>
  )
}

export default IterationBadge
