import { useMemo, useState } from 'react'
import { AdviserBar } from './adviser/AdviserBar'
import { useAdviserId } from './adviser/useAdviserId'
import { ApiContext } from './api/ApiContext'
import { createApiClient, DEFAULT_API_BASE_URL } from './api/client'
import { CreateQuote } from './quotes/CreateQuote'
import { QuoteDetail } from './quotes/QuoteDetail'
import { QuoteList } from './quotes/QuoteList'

const baseUrl = import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL

type View = { name: 'list' } | { name: 'create' } | { name: 'detail'; identifier: string }

export function App() {
  const [adviserId, setAdviserId] = useAdviserId()
  const [view, setView] = useState<View>({ name: 'list' })
  const api = useMemo(() => createApiClient({ baseUrl, adviserId }), [adviserId])

  const showList = () => setView({ name: 'list' })
  const showQuote = (identifier: string) => setView({ name: 'detail', identifier })

  return (
    <ApiContext.Provider value={api}>
      <AdviserBar adviserId={adviserId} onChange={setAdviserId} />
      <main>
        {!adviserId ? (
          <p className="notice">Enter your adviser id above to see and create quotes.</p>
        ) : view.name === 'create' ? (
          <CreateQuote onOpen={showQuote} onCancel={showList} />
        ) : view.name === 'detail' ? (
          <QuoteDetail identifier={view.identifier} onBack={showList} />
        ) : (
          <QuoteList onOpen={showQuote} onCreate={() => setView({ name: 'create' })} />
        )}
      </main>
    </ApiContext.Provider>
  )
}
