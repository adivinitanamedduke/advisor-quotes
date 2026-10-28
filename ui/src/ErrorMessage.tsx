import { errorDetails } from './api/client'
import type { ErrorDetail } from './api/types'

interface Props {
  error: unknown
  /** Field errors to list; defaults to all of them. Forms pass the ones they cannot show inline. */
  details?: ErrorDetail[]
}

/** Shows an API error: its message, then each field error the server listed. */
export function ErrorMessage({ error, details = errorDetails(error) }: Props) {
  if (!error) return null

  const message = error instanceof Error ? error.message : 'Something went wrong.'

  return (
    <div role="alert" className="error">
      <p>{message}</p>
      {details.length > 0 && (
        <ul>
          {details.map((detail, index) => (
            <li key={index}>
              {detail.field ? <strong>{detail.field}: </strong> : null}
              {detail.message}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
