import { useState } from 'react'

interface Props {
  adviserId: string
  onChange: (value: string) => void
}

/** Adviser id input. The id is applied on blur or Enter, not per keystroke, so screens reload once. */
export function AdviserBar({ adviserId, onChange }: Props) {
  const [draft, setDraft] = useState(adviserId)

  const apply = () => {
    const value = draft.trim()
    setDraft(value)
    if (value !== adviserId) onChange(value)
  }

  return (
    <header className="topbar">
      <h1>Advisor Quotes</h1>
      <label className="adviser">
        Adviser
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onBlur={apply}
          onKeyDown={(e) => {
            if (e.key === 'Enter') apply()
          }}
          placeholder="e.g. ADV-001"
          maxLength={100}
        />
      </label>
    </header>
  )
}
