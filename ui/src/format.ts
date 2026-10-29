export function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat('en-ZA', { style: 'currency', currency }).format(amount)
  } catch {
    // Unknown or empty currency code: show the number and the code as sent.
    return `${amount.toFixed(2)} ${currency}`.trim()
  }
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso)
  return Number.isNaN(date.getTime())
    ? iso
    : date.toLocaleString('en-ZA', { dateStyle: 'medium', timeStyle: 'short' })
}

/** "SentToClient" -> "Sent to client" */
export function statusLabel(status: string): string {
  const words = status.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return words.charAt(0).toUpperCase() + words.slice(1)
}

// Keys match PremiumCalculator.FactorNames in the API
const factorLabels: Record<string, string> = {
  coverUnits: 'Cover units (per R1 000)',
  baseRatePer1000: 'Base rate per R1 000',
  ageAtQuote: 'Age at quote date',
  ageFactor: 'Age factor',
  smokerFactor: 'Smoker loading',
  termFactor: 'Term factor',
}

/** "smokerFactor" -> "Smoker loading"; unknown keys fall back to "Some key" */
export function factorLabel(name: string): string {
  return factorLabels[name] ?? statusLabel(name)
}
