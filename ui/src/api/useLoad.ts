import { useCallback, useEffect, useState } from 'react'

interface LoadState<T> {
  load: () => Promise<T>
  data?: T
  error?: unknown
}

/**
 * Runs `load` whenever its identity changes (wrap it in useCallback) and ignores results that
 * arrive after a newer load started. `loading` is true until the current load settles.
 */
export function useLoad<T>(load: () => Promise<T>) {
  const [state, setState] = useState<LoadState<T>>()

  useEffect(() => {
    let active = true
    load().then(
      (data) => active && setState({ load, data }),
      (error: unknown) => active && setState({ load, error }),
    )
    return () => {
      active = false
    }
  }, [load])

  const setData = useCallback((data: T) => setState({ load, data }), [load])

  const current = state?.load === load ? state : undefined
  return { data: current?.data, error: current?.error, loading: !current, setData }
}
