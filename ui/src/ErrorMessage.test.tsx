import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from './api/client'
import { ErrorMessage } from './ErrorMessage'

describe('ErrorMessage', () => {
  it('ApiErrorWithTraceId_ShowsTraceId', () => {
    // Arrange
    const error = new ApiError(500, 'An unexpected error occurred', null, 'abc123')

    // Act
    render(<ErrorMessage error={error} />)

    // Assert
    expect(screen.getByRole('alert')).toHaveTextContent('Trace id: abc123')
  })

  it('ApiErrorWithoutTraceId_OmitsTraceLine', () => {
    // Arrange
    const error = new ApiError(0, 'Could not reach the API.')

    // Act
    render(<ErrorMessage error={error} />)

    // Assert
    expect(screen.getByRole('alert')).not.toHaveTextContent('Trace id')
  })

  it('FieldErrors_ListsEachWithField', () => {
    // Arrange
    const error = new ApiError(400, 'Validation failed', {
      statusCode: 400,
      message: 'Validation failed',
      errors: [{ field: 'Status', message: 'Status is required' }],
    })

    // Act
    render(<ErrorMessage error={error} />)

    // Assert
    expect(screen.getByRole('listitem')).toHaveTextContent('Status: Status is required')
  })

  it('NoError_RendersNothing', () => {
    // Act
    const { container } = render(<ErrorMessage error={null} />)

    // Assert
    expect(container).toBeEmptyDOMElement()
  })
})
