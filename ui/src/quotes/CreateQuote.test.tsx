import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { ProductDto, QuoteDto } from '../api/types'
import { renderWithApi } from '../test/renderWithApi'
import { CreateQuote } from './CreateQuote'

const termLife: ProductDto = {
  code: 'TERM_LIFE',
  name: 'Term Life',
  minCoverAmount: 100000,
  maxCoverAmount: 10000000,
  minTermYears: 5,
  maxTermYears: 30,
  minEntryAge: 18,
  maxEntryAge: 65,
}

const quote = { identifier: 'q-1', reference: 'QT-000042', monthlyPremium: 250, currency: 'ZAR' } as QuoteDto

async function renderForm(createQuote = vi.fn()) {
  const utils = renderWithApi(<CreateQuote onOpen={vi.fn()} onCancel={vi.fn()} />, {
    getProducts: vi.fn().mockResolvedValue([termLife]),
    createQuote,
  })
  await screen.findByRole('option', { name: 'Term Life' })
  return utils
}

async function fillValidForm() {
  await userEvent.type(screen.getByLabelText(/client full name/i), 'Jane Doe')
  await userEvent.type(screen.getByLabelText(/date of birth/i), '1990-05-01')
  await userEvent.type(screen.getByLabelText(/cover amount/i), '500000')
  await userEvent.type(screen.getByLabelText(/term \(years\)/i), '20')
}

const submit = () => userEvent.click(screen.getByRole('button', { name: 'Create quote' }))

describe('CreateQuote', () => {
  it('Submit_ValidForm_SendsRequestAndShowsCreatedQuote', async () => {
    // Arrange
    const createQuote = vi.fn().mockResolvedValue({ quote, replayed: false })
    await renderForm(createQuote)
    await fillValidForm()

    // Act
    await submit()

    // Assert
    expect(createQuote).toHaveBeenCalledWith(
      {
        client: { fullName: 'Jane Doe', dateOfBirth: '1990-05-01', isSmoker: false },
        product: { code: 'TERM_LIFE', coverAmount: 500000, termYears: 20 },
        notes: null,
      },
      expect.any(String),
    )
    expect(await screen.findByRole('status')).toHaveTextContent('Quote QT-000042 created')
  })

  it('Submit_ReplayedResponse_ShowsReplayNotice', async () => {
    // Arrange
    const createQuote = vi.fn().mockResolvedValue({ quote, replayed: true })
    await renderForm(createQuote)
    await fillValidForm()

    // Act
    await submit()

    // Assert
    expect(await screen.findByRole('status')).toHaveTextContent(/already submitted.*QT-000042/)
  })

  it('Submit_ServerValidationErrors_ShowsThemInlineAndInSummary', async () => {
    // Arrange
    const createQuote = vi.fn().mockRejectedValue(
      new ApiError(400, 'Validation failed', {
        statusCode: 400,
        message: 'Validation failed',
        errors: [
          { field: 'Client.FullName', message: 'Client full name is required' },
          { field: null, message: 'Client age 70 is outside the entry ages for Term Life' },
        ],
      }),
    )
    await renderForm(createQuote)

    // Act
    await submit()

    // Assert
    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('Validation failed')
    expect(alert).toHaveTextContent('Client age 70 is outside the entry ages for Term Life')
    expect(alert).not.toHaveTextContent('Client full name is required')
    expect(screen.getByText('Client full name is required')).toHaveClass('field-error')
  })

  // Property paths exactly as CreateQuoteRequestValidator reports them.
  it.each([
    ['Client.FullName', /client full name/i],
    ['Client.DateOfBirth', /date of birth/i],
    ['Product.Code', /product/i],
    ['Product.CoverAmount', /cover amount/i],
    ['Product.TermYears', /term \(years\)/i],
    ['Notes', /notes/i],
  ])('Submit_ValidatorField_%s_ShowsErrorOnItsInput', async (field, label) => {
    // Arrange
    const message = `${field} is wrong`
    const createQuote = vi.fn().mockRejectedValue(
      new ApiError(400, 'Validation failed', { statusCode: 400, message: 'Validation failed', errors: [{ field, message }] }),
    )
    await renderForm(createQuote)

    // Act
    await submit()

    // Assert
    expect(await screen.findByRole('alert')).not.toHaveTextContent(message)
    expect(screen.getByLabelText(label).closest('label')).toHaveTextContent(message)
  })

  it('Submit_RetryAfterNetworkError_ReusesIdempotencyKey', async () => {
    // Arrange
    const createQuote = vi
      .fn()
      .mockRejectedValueOnce(new ApiError(0, 'Could not reach the API.'))
      .mockResolvedValueOnce({ quote, replayed: false })
    await renderForm(createQuote)
    await fillValidForm()

    // Act
    await submit()
    await screen.findByRole('alert')
    await submit()

    // Assert
    expect(createQuote).toHaveBeenCalledTimes(2)
    expect(createQuote.mock.calls[1][1]).toBe(createQuote.mock.calls[0][1])
  })

  it('Submit_FormEditedAfterFailure_UsesNewIdempotencyKey', async () => {
    // Arrange
    const createQuote = vi.fn().mockRejectedValue(new ApiError(0, 'Could not reach the API.'))
    await renderForm(createQuote)
    await fillValidForm()
    await submit()
    await screen.findByRole('alert')

    // Act
    await userEvent.type(screen.getByLabelText(/notes/i), 'Prefers email')
    await submit()

    // Assert
    expect(createQuote).toHaveBeenCalledTimes(2)
    expect(createQuote.mock.calls[1][1]).not.toBe(createQuote.mock.calls[0][1])
  })

  it('Submit_InFlight_DisablesButton', async () => {
    // Arrange
    const createQuote = vi.fn(() => new Promise<never>(() => {}))
    await renderForm(createQuote)
    await fillValidForm()

    // Act
    await submit()

    // Assert
    expect(screen.getByRole('button', { name: 'Creating…' })).toBeDisabled()
  })

  it('ProductsFail_ShowsError', async () => {
    // Arrange / Act
    renderWithApi(<CreateQuote onOpen={vi.fn()} onCancel={vi.fn()} />, {
      getProducts: vi.fn().mockRejectedValue(new ApiError(500, 'An unexpected error occurred')),
    })

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('An unexpected error occurred')
  })
})
