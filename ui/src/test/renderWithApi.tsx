import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { vi, type Mock } from 'vitest'
import { ApiContext } from '../api/ApiContext'
import type { ApiClient } from '../api/client'

export type FakeApi = { [K in keyof ApiClient]: Mock<ApiClient[K]> }

/** Renders `ui` with an ApiClient whose methods are vi.fn() mocks; pass overrides to stub them. */
export function renderWithApi(ui: ReactElement, overrides: Partial<FakeApi> = {}) {
  const api: FakeApi = {
    getProducts: vi.fn<ApiClient['getProducts']>().mockResolvedValue([]),
    searchQuotes: vi.fn<ApiClient['searchQuotes']>(),
    getQuote: vi.fn<ApiClient['getQuote']>(),
    createQuote: vi.fn<ApiClient['createQuote']>(),
    updateQuoteStatus: vi.fn<ApiClient['updateQuoteStatus']>(),
    ...overrides,
  }
  const result = render(<ApiContext.Provider value={api}>{ui}</ApiContext.Provider>)
  return { api, ...result }
}
