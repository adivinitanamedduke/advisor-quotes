import type { QuoteStatus } from '../api/types'

// Mirrors the API's status machine (docs/ASSUMPTIONS.md A-05). The API still enforces it; this only
// decides which buttons to offer, since QuoteDto does not list the allowed next statuses.
const nextStatuses: Record<QuoteStatus, QuoteStatus[]> = {
  Draft: ['SentToClient', 'Declined'],
  SentToClient: ['Accepted', 'Declined'],
  Accepted: [],
  Declined: [],
}

export function allowedNextStatuses(status: QuoteStatus): QuoteStatus[] {
  return nextStatuses[status] ?? []
}
