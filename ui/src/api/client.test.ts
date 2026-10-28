import { describe, expect, it, vi } from 'vitest'
import { ApiError, createApiClient } from './client'
import type { CreateQuoteRequest } from './types'

function jsonResponse(body: unknown, status = 200, headers: Record<string, string> = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  })
}

const request: CreateQuoteRequest = {
  client: { fullName: 'Jane Doe', dateOfBirth: '1990-01-01', isSmoker: false },
  product: { code: 'TERM_LIFE', coverAmount: 500000, termYears: 20 },
}

describe('createApiClient', () => {
  it('searchQuotes_PostsBodyWithAdviserHeader_ReturnsPage', async () => {
    // Arrange
    const page = { items: [], totalCount: 0, pageNumber: 1, pageSize: 20 }
    const fetchImpl = vi.fn().mockResolvedValue(jsonResponse(page))
    const api = createApiClient({ baseUrl: 'http://api.test/', adviserId: 'ADV-1', fetchImpl })

    // Act
    const result = await api.searchQuotes({ status: 'Draft', pageNumber: 1, pageSize: 20 })

    // Assert
    expect(result).toEqual(page)
    const [url, init] = fetchImpl.mock.calls[0]
    expect(url).toBe('http://api.test/api/v1/Quote/search')
    expect(init.method).toBe('POST')
    expect(init.headers['X-Adviser-Id']).toBe('ADV-1')
    expect(JSON.parse(init.body)).toEqual({ status: 'Draft', pageNumber: 1, pageSize: 20 })
  })

  it('createQuote_SendsIdempotencyKey_ReportsReplayHeader', async () => {
    // Arrange
    const fetchImpl = vi.fn().mockResolvedValue(jsonResponse({ reference: 'QT-000001' }, 200, { 'Idempotent-Replayed': 'true' }))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    const result = await api.createQuote(request, 'key-123')

    // Assert
    expect(result.replayed).toBe(true)
    expect(result.quote.reference).toBe('QT-000001')
    expect(fetchImpl.mock.calls[0][1].headers['Idempotency-Key']).toBe('key-123')
  })

  it('createQuote_NoReplayHeader_ReportsNotReplayed', async () => {
    // Arrange
    const fetchImpl = vi.fn().mockResolvedValue(jsonResponse({ reference: 'QT-000001' }, 201))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    const result = await api.createQuote(request, 'key-123')

    // Assert
    expect(result.replayed).toBe(false)
  })

  it('send_ErrorResponse_ThrowsApiErrorWithBody', async () => {
    // Arrange
    const body = { statusCode: 400, message: 'Validation failed', errors: [{ field: 'Client.FullName', message: 'Required' }] }
    const fetchImpl = vi.fn().mockResolvedValue(jsonResponse(body, 400))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    const error = await api.createQuote(request, 'key').catch((e) => e)

    // Assert
    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(400)
    expect(error.message).toBe('Validation failed')
    expect(error.body.errors).toEqual(body.errors)
  })

  it('send_NonJsonError_ThrowsApiErrorWithStatus', async () => {
    // Arrange
    const fetchImpl = vi.fn().mockResolvedValue(new Response('oops', { status: 502 }))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    const error = await api.getProducts().catch((e) => e)

    // Assert
    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(502)
    expect(error.body).toBeNull()
  })

  it('send_NetworkFailure_ThrowsApiErrorWithStatusZero', async () => {
    // Arrange
    const fetchImpl = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    const error = await api.getQuote('abc').catch((e) => e)

    // Assert
    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(0)
  })

  it('updateQuoteStatus_PutsToStatusRoute', async () => {
    // Arrange
    const fetchImpl = vi.fn().mockResolvedValue(jsonResponse({ status: 'Declined' }))
    const api = createApiClient({ baseUrl: 'http://api.test', adviserId: 'ADV-1', fetchImpl })

    // Act
    await api.updateQuoteStatus('q-1', { status: 'Declined', reason: 'Too expensive' })

    // Assert
    const [url, init] = fetchImpl.mock.calls[0]
    expect(url).toBe('http://api.test/api/v1/Quote/q-1/status')
    expect(init.method).toBe('PUT')
  })
})
