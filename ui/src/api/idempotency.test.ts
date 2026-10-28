import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useIdempotencyKey } from './idempotency'

describe('useIdempotencyKey', () => {
  it('keyFor_SameBodyTwice_ReusesKey', () => {
    // Arrange
    const { result } = renderHook(() => useIdempotencyKey())

    // Act
    const first = result.current.keyFor({ a: 1 })
    const second = result.current.keyFor({ a: 1 })

    // Assert
    expect(second).toBe(first)
  })

  it('keyFor_ChangedBody_IssuesNewKey', () => {
    // Arrange
    const { result } = renderHook(() => useIdempotencyKey())

    // Act
    const first = result.current.keyFor({ a: 1 })
    const second = result.current.keyFor({ a: 2 })

    // Assert
    expect(second).not.toBe(first)
  })

  it('keyFor_AfterReset_IssuesNewKey', () => {
    // Arrange
    const { result } = renderHook(() => useIdempotencyKey())
    const first = result.current.keyFor({ a: 1 })

    // Act
    result.current.reset()
    const second = result.current.keyFor({ a: 1 })

    // Assert
    expect(second).not.toBe(first)
  })
})
