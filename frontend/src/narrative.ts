// Step 140: the narrative of a story in the language of the project, the same
// as the exports write it (ExportLanguage.Narrative in C#). The three fields
// are saved without the words around them:
//   es  «Como empleado, quiero reservar una sala, para tenerla asegurada.»
//   en  «As an employee, I want to book a room, so that I can have it secured.»
// In English the model writes the role with its article and "quiero" and
// "para" starting with a verb, so "to" and "I can" go around them.

type Words = { as: string; want: string; soThat: string }

export function storyWords(language: string): Words {
  return language === 'en' ? { as: 'As', want: 'I want to', soThat: 'so that I can' } : { as: 'Como', want: 'quiero', soThat: 'para' }
}

// What the role wants, without a "to" the model already wrote in English.
export function wantText(language: string, want: string): string {
  const trimmed = want.trim()
  return language === 'en' && trimmed.toLowerCase().startsWith('to ') ? trimmed.slice(3) : want
}

// What for, without the final full stop in English (the narrative adds it).
export function soThatText(language: string, soThat: string): string {
  return language === 'en' ? soThat.trim().replace(/\.+$/, '') : soThat
}

// "Como X, quiero Y, para Z." or, without soThat, "Como X, quiero Y".
export function narrative(language: string, role: string, want: string, soThat?: string): string {
  const w = storyWords(language)
  const start = `${w.as} ${role}, ${w.want} ${wantText(language, want)}`
  return soThat === undefined ? start : `${start}, ${w.soThat} ${soThatText(language, soThat)}.`
}
