import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { QuoteDto, QuoteStatus } from '../api/types'
import { renderWithApi } from '../test/renderWithApi'
import { QuoteDetail } from './QuoteDetail'

function quote(status: QuoteStatus): QuoteDto {
  return {
    identifier: 'q-1',
    reference: 'QT-000007',
    adviserId: 'ADV-1',
    client: { fullName: 'Jane Doe', dateOfBirth: '1990-05-01', isSmoker: true },
    product: { code: 'TERM_LIFE', coverAmount: 500000, termYears: 20 },
    monthlyPremium: 412.35,
    currency: 'ZAR',
    pricingVersion: '2026-10',
    ratingFactors: { age: 1.12, smoker: 1.5 },
    status,
    notes: null,
    createdAt: '2026-10-01T10:00:00Z',
    updatedAt: '2026-10-01T10:00:00Z',
    statusHistory: [{ fromStatus: null, toStatus: 'Draft', reason: null, changedBy: 'ADV-1', changedAt: '2026-10-01T10:00:00Z' }],
  }
}

function renderDetail(current: QuoteDto, updateQuoteStatus = vi.fn()) {
  return renderWithApi(<QuoteDetail identifier="q-1" onBack={vi.fn()} />, {
    getQuote: vi.fn().mockResolvedValue(current),
    updateQuoteStatus,
  })
}

const statusButtons = () => screen.queryAllByRole('button', { name: /^Mark as/ }).map((b) => b.textContent)

describe('QuoteDetail', () => {
  it('Load_ShowsPremiumFactorsAndHistory', async () => {
    // Arrange / Act
    renderDetail(quote('Draft'))

    // Assert
    expect(await screen.findByText('QT-000007')).toBeInTheDocument()
    expect(screen.getByText(/412,35|412\.35/)).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'smoker' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: '1.5' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'ADV-1' })).toBeInTheDocument()
  })

  it.each<[QuoteStatus, string[]]>([
    ['Draft', ['Mark as sent to client', 'Mark as declined']],
    ['SentToClient', ['Mark as accepted', 'Mark as declined']],
    ['Accepted', []],
    ['Declined', []],
  ])('StatusButtons_%s_OffersOnlyAllowedNextStatuses', async (status, expected) => {
    // Arrange / Act
    renderDetail(quote(status))
    await screen.findByText('QT-000007')

    // Assert
    expect(statusButtons()).toEqual(expected)
  })

  it('StatusButtons_FinalStatus_ExplainsWhy', async () => {
    // Arrange / Act
    renderDetail(quote('Accepted'))

    // Assert
    expect(await screen.findByText(/status is final/)).toBeInTheDocument()
  })

  it('StatusButton_Clicked_SendsStatusAndReasonThenShowsUpdatedQuote', async () => {
    // Arrange
    const updateQuoteStatus = vi.fn().mockResolvedValue(quote('SentToClient'))
    renderDetail(quote('Draft'), updateQuoteStatus)
    await userEvent.type(await screen.findByLabelText(/reason/i), 'Emailed to client')

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Mark as sent to client' }))

    // Assert
    expect(updateQuoteStatus).toHaveBeenCalledWith('q-1', { status: 'SentToClient', reason: 'Emailed to client' })
    expect(await screen.findByRole('button', { name: 'Mark as accepted' })).toBeInTheDocument()
    expect(screen.getByLabelText(/reason/i)).toHaveValue('')
  })

  it('StatusButton_NoReason_SendsNullReason', async () => {
    // Arrange
    const updateQuoteStatus = vi.fn().mockResolvedValue(quote('Declined'))
    renderDetail(quote('Draft'), updateQuoteStatus)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Mark as declined' }))

    // Assert
    expect(updateQuoteStatus).toHaveBeenCalledWith('q-1', { status: 'Declined', reason: null })
  })

  it('StatusChangeRejected_ShowsServerError', async () => {
    // Arrange
    const updateQuoteStatus = vi
      .fn()
      .mockRejectedValue(new ApiError(409, 'The quote was changed by someone else. Reload and try again.'))
    renderDetail(quote('Draft'), updateQuoteStatus)

    // Act
    await userEvent.click(await screen.findByRole('button', { name: 'Mark as declined' }))

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('changed by someone else')
  })

  it('UnknownQuote_ShowsNotFound', async () => {
    // Arrange / Act
    renderWithApi(<QuoteDetail identifier="missing" onBack={vi.fn()} />, {
      getQuote: vi.fn().mockRejectedValue(new ApiError(404, 'Quote not found')),
    })

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Quote not found')
  })
})
