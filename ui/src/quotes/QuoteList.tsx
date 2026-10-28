import { useCallback, useState } from 'react'
import { useApi } from '../api/ApiContext'
import { QUOTE_STATUSES, type QuoteStatus } from '../api/types'
import { useLoad } from '../api/useLoad'
import { ErrorMessage } from '../ErrorMessage'
import { formatDateTime, formatMoney, statusLabel } from '../format'

export const PAGE_SIZE = 10

interface Props {
  onOpen: (identifier: string) => void
  onCreate: () => void
}

export function QuoteList({ onOpen, onCreate }: Props) {
  const api = useApi()
  const [status, setStatus] = useState<QuoteStatus | ''>('')
  const [pageNumber, setPageNumber] = useState(1)

  const search = useCallback(
    () => api.searchQuotes({ status: status || undefined, pageNumber, pageSize: PAGE_SIZE }),
    [api, status, pageNumber],
  )
  const { data, error, loading } = useLoad(search)

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1

  return (
    <section>
      <div className="toolbar">
        <h2>Quotes</h2>
        <div className="actions">
          <label className="field inline">
            <span>Status</span>
            <select
              value={status}
              onChange={(e) => {
                setStatus(e.target.value as QuoteStatus | '')
                setPageNumber(1)
              }}
            >
              <option value="">All</option>
              {QUOTE_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {statusLabel(s)}
                </option>
              ))}
            </select>
          </label>
          <button type="button" onClick={onCreate}>
            New quote
          </button>
        </div>
      </div>

      <ErrorMessage error={error} />
      {loading && <p className="muted">Loading quotes…</p>}

      {data && data.items.length === 0 && <p className="notice">No quotes found.</p>}

      {data && data.items.length > 0 && (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Reference</th>
                  <th>Client</th>
                  <th>Product</th>
                  <th className="number">Cover</th>
                  <th className="number">Monthly premium</th>
                  <th>Status</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((quote) => (
                  <tr key={quote.identifier}>
                    <td>
                      <button type="button" className="link" onClick={() => onOpen(quote.identifier)}>
                        {quote.reference}
                      </button>
                    </td>
                    <td>{quote.clientFullName}</td>
                    <td>{quote.productCode}</td>
                    <td className="number">{formatMoney(quote.coverAmount, quote.currency)}</td>
                    <td className="number">{formatMoney(quote.monthlyPremium, quote.currency)}</td>
                    <td>
                      <span className="status">{statusLabel(quote.status)}</span>
                    </td>
                    <td>{formatDateTime(quote.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <nav className="pager" aria-label="Pagination">
            <button
              type="button"
              className="secondary"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber((p) => p - 1)}
            >
              Previous
            </button>
            <span>
              Page {data.pageNumber} of {totalPages} ({data.totalCount} quotes)
            </span>
            <button
              type="button"
              className="secondary"
              disabled={pageNumber >= totalPages}
              onClick={() => setPageNumber((p) => p + 1)}
            >
              Next
            </button>
          </nav>
        </>
      )}
    </section>
  )
}
