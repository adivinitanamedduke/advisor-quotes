import { ApiError } from './api/client'

/** Shows an API error: its message, then each field error the server listed. */
export function ErrorMessage({ error }: { error: unknown }) {
  if (!error) return null

  const message = error instanceof Error ? error.message : 'Something went wrong.'
  const details = error instanceof ApiError ? (error.body?.errors ?? []) : []

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
