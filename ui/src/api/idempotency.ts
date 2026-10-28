import { useCallback, useRef } from 'react'

/**
 * One Idempotency-Key per submission attempt. Submitting the same body again (a retry after a
 * network error, or a double click) reuses the key so the API can recognise the repeat; a changed
 * body gets a fresh key. Call reset() once the submission has succeeded.
 */
export function useIdempotencyKey() {
  const pending = useRef<{ key: string; body: string } | null>(null)

  const keyFor = useCallback((request: unknown) => {
    const body = JSON.stringify(request)
    if (pending.current?.body !== body) {
      pending.current = { key: crypto.randomUUID(), body }
    }
    return pending.current.key
  }, [])

  const reset = useCallback(() => {
    pending.current = null
  }, [])

  return { keyFor, reset }
}
