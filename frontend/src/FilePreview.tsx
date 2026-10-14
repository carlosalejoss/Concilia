import type { ReactNode } from 'react'
import Markdown, { type Components } from 'react-markdown'
import remarkGfm from 'remark-gfm'
import DocumentText from './DocumentText.tsx'

// The reader of a project file: the text extracted from it, shown the way its
// type is written. Markdown is rendered as Markdown; data files (JSON, YAML,
// XML) as code, with their indentation; everything else (Word, PDF, sheets,
// slides, emails, transcripts...) with DocumentText, which arranges the plain
// text that the extraction leaves.

const DATA_EXTENSIONS = ['.json', '.yaml', '.yml', '.xml']

function FilePreview({ fileName, text }: { fileName: string; text: string }) {
  const dot = fileName.lastIndexOf('.')
  const extension = dot >= 0 ? fileName.slice(dot).toLowerCase() : ''

  if (extension === '.md') return <MarkdownView text={text} />
  if (DATA_EXTENSIONS.includes(extension)) return <CodeView text={text} />
  return <DocumentText text={text} />
}

function CodeView({ text }: { text: string }) {
  return <pre className="overflow-x-auto rounded border border-line bg-sunken p-3 font-mono text-xs leading-relaxed">{text}</pre>
}

// Each Markdown element with the styles of the rest of the reader. Raw HTML
// inside the Markdown is shown as text, not run (react-markdown's default),
// and images are not loaded: only their description is shown.
const MARKDOWN: Components = {
  h1: ({ children }) => <h3 className="mt-8 font-sans text-xl font-semibold">{children}</h3>,
  h2: ({ children }) => <h4 className="mt-6 font-sans text-base font-semibold">{children}</h4>,
  h3: ({ children }) => <h5 className="mt-4 font-sans text-sm font-semibold">{children}</h5>,
  h4: ({ children }) => <h6 className="mt-4 font-sans text-sm font-semibold text-muted">{children}</h6>,
  h5: ({ children }) => <h6 className="mt-4 font-sans text-sm font-semibold text-muted">{children}</h6>,
  h6: ({ children }) => <h6 className="mt-4 font-sans text-sm font-semibold text-muted">{children}</h6>,
  p: ({ children }) => <p className="mt-3">{children}</p>,
  ul: ({ children, className }) => (
    // GFM task lists ("- [x] ...") carry their own checkbox: no bullet.
    <ul className={`mt-3 space-y-1 ${className?.includes('contains-task-list') ? 'list-none pl-1' : 'list-disc pl-5'} [&_ol]:mt-1 [&_ul]:mt-1`}>{children}</ul>
  ),
  ol: ({ children, start }) => (
    <ol start={start} className="mt-3 list-decimal space-y-1 pl-5 [&_ol]:mt-1 [&_ul]:mt-1">
      {children}
    </ol>
  ),
  input: ({ checked }) => <input type="checkbox" checked={checked} disabled className="mr-1.5 align-middle accent-accent" />,
  blockquote: ({ children }) => <blockquote className="mt-3 border-l-2 border-line pl-4 text-muted [&>p:first-child]:mt-0">{children}</blockquote>,
  hr: () => <hr className="mt-6 border-line" />,
  a: ({ children, href }) => (
    <a href={href} target="_blank" rel="noreferrer" className="text-accent underline underline-offset-2 hover:text-accent-hover">
      {children}
    </a>
  ),
  img: ({ alt }) => <span className="font-sans text-sm text-faint">[Imagen{alt ? `: ${alt}` : ''}]</span>,
  // Inline code; a code block is a <pre> around it and gets its styles there.
  code: ({ children }) => <code className="rounded bg-sunken px-1 py-0.5 font-mono text-[0.85em] [pre_&]:bg-transparent [pre_&]:p-0 [pre_&]:text-[1em]">{children}</code>,
  pre: ({ children }) => <pre className="mt-3 overflow-x-auto rounded border border-line bg-sunken p-3 text-xs leading-relaxed">{children}</pre>,
  table: ({ children }) => (
    <div className="mt-4 overflow-x-auto">
      <table className="w-full border-collapse font-sans text-xs">{children}</table>
    </div>
  ),
  thead: ({ children }) => <thead className="bg-sunken font-medium">{children}</thead>,
  tr: ({ children }) => <tr className="border-t border-line first:border-t-0">{children}</tr>,
  th: ({ children, style }) => (
    <th style={style} className="px-2 py-1.5 text-left align-top">
      {children}
    </th>
  ),
  td: ({ children, style }) => (
    <td style={style} className="px-2 py-1.5 align-top">
      {children}
    </td>
  ),
}

function MarkdownView({ text }: { text: string }): ReactNode {
  return (
    <article className="max-w-prose font-serif leading-relaxed [&>*:first-child]:mt-0">
      <Markdown remarkPlugins={[remarkGfm]} components={MARKDOWN}>
        {text}
      </Markdown>
    </article>
  )
}

export default FilePreview
