// Mirrors the DTOs in Contracts/ (ASP.NET serialises them camelCase).
// DateOnly travels as "yyyy-MM-dd", DateTime as ISO 8601 UTC, decimal as number.

export const QUOTE_STATUSES = ['Draft', 'SentToClient', 'Accepted', 'Declined'] as const
export type QuoteStatus = (typeof QUOTE_STATUSES)[number]

export interface ErrorDetail {
  field?: string | null
  message: string
}

export interface ErrorResponse {
  statusCode: number
  message: string
  errors: ErrorDetail[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface ProductDto {
  code: string
  name: string
  minCoverAmount: number
  maxCoverAmount: number
  minTermYears: number
  maxTermYears: number
  minEntryAge: number
  maxEntryAge: number
}

export interface QuoteClient {
  fullName: string
  dateOfBirth: string
  isSmoker: boolean
}

export interface QuoteProduct {
  code: string
  coverAmount: number
  termYears: number
}

export interface CreateQuoteRequest {
  client: QuoteClient
  product: QuoteProduct
  notes?: string | null
}

export interface SearchQuotesRequest {
  status?: QuoteStatus | null
  pageNumber: number
  pageSize: number
}

export interface UpdateQuoteStatusRequest {
  status: QuoteStatus
  reason?: string | null
}

export interface QuoteStatusChangeDto {
  fromStatus?: QuoteStatus | null
  toStatus: QuoteStatus
  reason?: string | null
  changedBy: string
  changedAt: string
}

export interface QuoteDto {
  identifier: string
  reference: string
  adviserId: string
  client: QuoteClient
  product: QuoteProduct
  monthlyPremium: number
  currency: string
  pricingVersion: string
  ratingFactors: Record<string, number>
  status: QuoteStatus
  notes?: string | null
  createdAt: string
  updatedAt: string
  statusHistory: QuoteStatusChangeDto[]
}

export interface QuoteSummaryDto {
  identifier: string
  reference: string
  clientFullName: string
  productCode: string
  coverAmount: number
  monthlyPremium: number
  currency: string
  status: QuoteStatus
  createdAt: string
  updatedAt: string
}
