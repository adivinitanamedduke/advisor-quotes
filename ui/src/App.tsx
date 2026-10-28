import { useMemo } from 'react'
import { AdviserBar } from './adviser/AdviserBar'
import { useAdviserId } from './adviser/useAdviserId'
import { ApiContext } from './api/ApiContext'
import { createApiClient, DEFAULT_API_BASE_URL } from './api/client'

const baseUrl = import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL

export function App() {
  const [adviserId, setAdviserId] = useAdviserId()
  const api = useMemo(() => createApiClient({ baseUrl, adviserId }), [adviserId])

  return (
    <ApiContext.Provider value={api}>
      <AdviserBar adviserId={adviserId} onChange={setAdviserId} />
      <main>
        {adviserId ? null : <p className="notice">Enter your adviser id above to see and create quotes.</p>}
      </main>
    </ApiContext.Provider>
  )
}
