import { useCallback, useState } from 'react'

export const ADVISER_STORAGE_KEY = 'advisorQuotes.adviserId'

function readStored(): string {
  try {
    return localStorage.getItem(ADVISER_STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

/** The adviser id sent as X-Adviser-Id, remembered across reloads. */
export function useAdviserId() {
  const [adviserId, setState] = useState(readStored)

  const setAdviserId = useCallback((value: string) => {
    setState(value)
    try {
      if (value) localStorage.setItem(ADVISER_STORAGE_KEY, value)
      else localStorage.removeItem(ADVISER_STORAGE_KEY)
    } catch {
      // Storage can be blocked (private windows); the id still works until reload.
    }
  }, [])

  return [adviserId, setAdviserId] as const
}
