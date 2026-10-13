import type { ReactNode } from 'react'

// The text extracted from a document, shown as a document: headings
// (Markdown "#" or numbered sections like "2.1 Reservas"), paragraphs, lists
// and Markdown tables, instead of one block of plain text. It only arranges
// what is there: nothing is added or removed.

type Block =
  | { kind: 'heading'; level: 1 | 2 | 3; text: string }
  | { kind: 'paragraph'; text: string }
  | { kind: 'list'; items: string[] }
  | { kind: 'table'; rows: string[][] }

const MARKDOWN_HEADING = /^(#{1,6})\s+(.*)$/
// "2.1 Reservas", "3. Usuarios": short, without a final period.
const NUMBERED_HEADING = /^(\d+(?:\.\d+)*\.?)\s+(\S.*)$/
const LIST_ITEM = /^(?:[-*•·]|\d+[.)])\s+(.*)$/

function isNumberedHeading(line: string): boolean {
  const match = NUMBERED_HEADING.exec(line)
  return !!match && match[2].split(/\s+/).length <= 10 && !/[.:;,]$/.test(match[2])
}

function cells(line: string): string[] {
  return line.replace(/^\|/, '').replace(/\|$/, '').split('|').map((c) => c.trim())
}

function toBlocks(text: string): Block[] {
  const blocks: Block[] = []
  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim()
    const last = blocks[blocks.length - 1]
    if (line === '') continue
    if (line.startsWith('|')) {
      if (/^\|?\s*:?-{2,}/.test(line)) continue // the |---|---| line of a Markdown table
      if (last?.kind === 'table') last.rows.push(cells(line))
      else blocks.push({ kind: 'table', rows: [cells(line)] })
      continue
    }
    const heading = MARKDOWN_HEADING.exec(line)
    if (heading) {
      blocks.push({ kind: 'heading', level: Math.min(heading[1].length, 3) as 1 | 2 | 3, text: heading[2] })
    } else if (isNumberedHeading(line)) {
      blocks.push({ kind: 'heading', level: line.split('.').filter(Boolean).length > 2 ? 3 : 2, text: line })
    } else if (LIST_ITEM.test(line)) {
      const item = LIST_ITEM.exec(line)![1]
      if (last?.kind === 'list') last.items.push(item)
      else blocks.push({ kind: 'list', items: [item] })
    } else {
      blocks.push({ kind: 'paragraph', text: line })
    }
  }
  return blocks
}

const HEADING_STYLES = {
  1: 'mt-8 font-sans text-xl font-semibold',
  2: 'mt-6 font-sans text-base font-semibold',
  3: 'mt-4 font-sans text-sm font-semibold',
}

function DocumentText({ text }: { text: string }) {
  const blocks = toBlocks(text)
  return (
    <article className="max-w-prose font-serif leading-relaxed [&>*:first-child]:mt-0">
      {blocks.map((block, i): ReactNode => {
        switch (block.kind) {
          case 'heading': {
            const Tag = block.level === 1 ? 'h3' : 'h4'
            return (
              <Tag key={i} className={HEADING_STYLES[block.level]}>
                {block.text}
              </Tag>
            )
          }
          case 'paragraph':
            return (
              <p key={i} className="mt-3">
                {block.text}
              </p>
            )
          case 'list':
            return (
              <ul key={i} className="mt-3 list-disc space-y-1 pl-5">
                {block.items.map((item, j) => (
                  <li key={j}>{item}</li>
                ))}
              </ul>
            )
          case 'table':
            return (
              <div key={i} className="mt-4 overflow-x-auto">
                <table className="w-full border-collapse font-sans text-xs">
                  <tbody>
                    {block.rows.map((row, j) => (
                      <tr key={j} className={j === 0 ? 'bg-sunken font-medium' : 'border-t border-line'}>
                        {row.map((cell, k) => (
                          <td key={k} className="px-2 py-1.5 align-top">
                            {cell}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
        }
      })}
    </article>
  )
}

export default DocumentText
