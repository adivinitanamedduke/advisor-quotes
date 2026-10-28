import type {
  CreateQuoteRequest,
  ErrorDetail,
  ErrorResponse,
  PagedResult,
  ProductDto,
  QuoteDto,
  QuoteSummaryDto,
  SearchQuotesRequest,
  UpdateQuoteStatusRequest,
} from './types'

export const DEFAULT_API_BASE_URL = 'http://localhost:5222'

/** A non-2xx response, or a request that never reached the API (status 0). */
export class ApiError extends Error {
  readonly status: number
  readonly body: ErrorResponse | null
  /** The API's X-Trace-Id for this request, for finding it in the logs. */
  readonly traceId: string | null

  constructor(status: number, message: string, body: ErrorResponse | null = null, traceId: string | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.body = body
    this.traceId = traceId
  }
}

/** The field errors an API error carries, or none for other errors. */
export function errorDetails(error: unknown): ErrorDetail[] {
  return error instanceof ApiError ? (error.body?.errors ?? []) : []
}

export interface CreateQuoteResult {
  quote: QuoteDto
  /** True when the API returned an existing quote for a repeated submission. */
  replayed: boolean
}

export interface ApiClient {
  getProducts(): Promise<ProductDto[]>
  searchQuotes(request: SearchQuotesRequest): Promise<PagedResult<QuoteSummaryDto>>
  getQuote(identifier: string): Promise<QuoteDto>
  createQuote(request: CreateQuoteRequest, idempotencyKey: string): Promise<CreateQuoteResult>
  updateQuoteStatus(identifier: string, request: UpdateQuoteStatusRequest): Promise<QuoteDto>
}

interface ClientOptions {
  baseUrl: string
  adviserId: string
  fetchImpl?: typeof fetch
}

export function createApiClient({ baseUrl, adviserId, fetchImpl = fetch }: ClientOptions): ApiClient {
  const root = baseUrl.replace(/\/+$/, '')

  async function send(method: string, path: string, body?: unknown, headers: Record<string, string> = {}) {
    let response: Response
    try {
      response = await fetchImpl(`${root}/api/v1/${path}`, {
        method,
        headers: {
          Accept: 'application/json',
          'X-Adviser-Id': adviserId,
          ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
          ...headers,
        },
        body: body === undefined ? undefined : JSON.stringify(body),
      })
    } catch {
      throw new ApiError(0, 'Could not reach the API. Check that it is running and try again.')
    }

    if (!response.ok) {
      const errorBody = await readErrorBody(response)
      throw new ApiError(
        response.status,
        errorBody?.message || `Request failed (${response.status})`,
        errorBody,
        response.headers.get('X-Trace-Id'),
      )
    }

    return response
  }

  return {
    async getProducts() {
      return (await send('GET', 'Product')).json()
    },
    async searchQuotes(request) {
      return (await send('POST', 'Quote/search', request)).json()
    },
    async getQuote(identifier) {
      return (await send('GET', `Quote/${encodeURIComponent(identifier)}`)).json()
    },
    async createQuote(request, idempotencyKey) {
      const response = await send('POST', 'Quote', request, { 'Idempotency-Key': idempotencyKey })
      return {
        quote: await response.json(),
        replayed: response.headers.get('Idempotent-Replayed')?.toLowerCase() === 'true',
      }
    },
    async updateQuoteStatus(identifier, request) {
      return (await send('PUT', `Quote/${encodeURIComponent(identifier)}/status`, request)).json()
    },
  }
}

async function readErrorBody(response: Response): Promise<ErrorResponse | null> {
  try {
    const body = await response.json()
    return body && typeof body.message === 'string' ? { ...body, errors: body.errors ?? [] } : null
  } catch {
    return null
  }
}
