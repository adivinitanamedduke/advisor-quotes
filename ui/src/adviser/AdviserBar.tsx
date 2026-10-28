interface Props {
  adviserId: string
  onChange: (value: string) => void
}

export function AdviserBar({ adviserId, onChange }: Props) {
  return (
    <header className="topbar">
      <h1>Advisor Quotes</h1>
      <label className="adviser">
        Adviser
        <input
          value={adviserId}
          onChange={(e) => onChange(e.target.value.trim())}
          placeholder="e.g. ADV-001"
          maxLength={100}
        />
      </label>
    </header>
  )
}
