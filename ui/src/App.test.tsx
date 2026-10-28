import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { ADVISER_STORAGE_KEY } from './adviser/useAdviserId'

describe('App', () => {
  beforeEach(() => {
    const emptyPage = { items: [], totalCount: 0, pageNumber: 1, pageSize: 10 }
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(emptyPage), { status: 200 })))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('AdviserInput_Typed_PersistsToLocalStorage', async () => {
    // Arrange
    render(<App />)

    // Act
    await userEvent.type(screen.getByLabelText('Adviser'), 'ADV-7')

    // Assert
    expect(localStorage.getItem(ADVISER_STORAGE_KEY)).toBe('ADV-7')
  })

  it('AdviserInput_StoredValue_IsRestored', () => {
    // Arrange
    localStorage.setItem(ADVISER_STORAGE_KEY, 'ADV-9')

    // Act
    render(<App />)

    // Assert
    expect(screen.getByLabelText('Adviser')).toHaveValue('ADV-9')
  })

  it('NoAdviser_ShowsPrompt', () => {
    // Act
    render(<App />)

    // Assert
    expect(screen.getByText(/enter your adviser id/i)).toBeInTheDocument()
  })
})
