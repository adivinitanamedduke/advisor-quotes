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

  it('AdviserInput_EnterPressed_PersistsToLocalStorage', async () => {
    // Arrange
    render(<App />)

    // Act
    await userEvent.type(screen.getByLabelText('Adviser'), 'ADV-7{Enter}')

    // Assert
    expect(localStorage.getItem(ADVISER_STORAGE_KEY)).toBe('ADV-7')
  })

  it('AdviserInput_Typing_DoesNotSearchPerKeystroke', async () => {
    // Arrange
    render(<App />)

    // Act
    await userEvent.type(screen.getByLabelText('Adviser'), 'ADV-7')

    // Assert
    expect(fetch).not.toHaveBeenCalled()
    expect(localStorage.getItem(ADVISER_STORAGE_KEY)).toBeNull()
  })

  it('AdviserInput_Blurred_SearchesOnceWithAdviserHeader', async () => {
    // Arrange
    render(<App />)
    await userEvent.type(screen.getByLabelText('Adviser'), ' ADV-7 ')

    // Act
    await userEvent.tab()

    // Assert
    expect(await screen.findByText('No quotes found.')).toBeInTheDocument()
    expect(fetch).toHaveBeenCalledTimes(1)
    const init = vi.mocked(fetch).mock.calls[0][1] as RequestInit
    expect((init.headers as Record<string, string>)['X-Adviser-Id']).toBe('ADV-7')
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
