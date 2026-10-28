import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { PagedResult, QuoteSummaryDto } from '../api/types'
import { renderWithApi } from '../test/renderWithApi'
import { PAGE_SIZE, QuoteList } from './QuoteList'

function summary(reference: string): QuoteSummaryDto {
  return {
    identifier: `id-${reference}`,
    reference,
    clientFullName: 'Jane Doe',
    productCode: 'TERM_LIFE',
    coverAmount: 500000,
    monthlyPremium: 312.5,
    currency: 'ZAR',
    status: 'Draft',
    createdAt: '2026-10-01T10:00:00Z',
    updatedAt: '2026-10-01T10:00:00Z',
  }
}

function page(items: QuoteSummaryDto[], totalCount = items.length, pageNumber = 1): PagedResult<QuoteSummaryDto> {
  return { items, totalCount, pageNumber, pageSize: PAGE_SIZE }
}

describe('QuoteList', () => {
  it('Load_RendersRowsFromFirstPage', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockResolvedValue(page([summary('QT-000001')]))

    // Act
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })

    // Assert
    expect(await screen.findByRole('button', { name: 'QT-000001' })).toBeInTheDocument()
    expect(searchQuotes).toHaveBeenCalledWith({ status: undefined, pageNumber: 1, pageSize: PAGE_SIZE })
  })

  it('StatusFilter_Changed_SearchesWithStatusFromPageOne', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockResolvedValue(page([summary('QT-000001')]))
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })
    await screen.findByText('QT-000001')

    // Act
    await userEvent.selectOptions(screen.getByLabelText('Status'), 'Accepted')

    // Assert
    expect(searchQuotes).toHaveBeenLastCalledWith({ status: 'Accepted', pageNumber: 1, pageSize: PAGE_SIZE })
  })

  it('Next_Clicked_RequestsNextPage', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockResolvedValue(page([summary('QT-000001')], 25))
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })
    await screen.findByText('Page 1 of 3 (25 quotes)')

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Next' }))

    // Assert
    expect(searchQuotes).toHaveBeenLastCalledWith({ status: undefined, pageNumber: 2, pageSize: PAGE_SIZE })
  })

  it('SinglePage_DisablesPagerButtons', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockResolvedValue(page([summary('QT-000001')]))

    // Act
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })

    // Assert
    expect(await screen.findByRole('button', { name: 'Previous' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled()
  })

  it('Reference_Clicked_OpensQuote', async () => {
    // Arrange
    const onOpen = vi.fn()
    const searchQuotes = vi.fn().mockResolvedValue(page([summary('QT-000001')]))
    renderWithApi(<QuoteList onOpen={onOpen} onCreate={vi.fn()} />, { searchQuotes })

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'QT-000001' }))

    // Assert
    expect(onOpen).toHaveBeenCalledWith('id-QT-000001')
  })

  it('NoQuotes_ShowsEmptyState', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockResolvedValue(page([]))

    // Act
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })

    // Assert
    expect(await screen.findByText('No quotes found.')).toBeInTheDocument()
  })

  it('SearchFails_ShowsError', async () => {
    // Arrange
    const searchQuotes = vi.fn().mockRejectedValue(new ApiError(400, 'X-Adviser-Id header is required'))

    // Act
    renderWithApi(<QuoteList onOpen={vi.fn()} onCreate={vi.fn()} />, { searchQuotes })

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('X-Adviser-Id header is required')
  })
})
