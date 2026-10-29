import { useCallback, useState } from 'react'
import { useApi } from '../api/ApiContext'
import type { QuoteDto, QuoteStatus } from '../api/types'
import { useLoad } from '../api/useLoad'
import { ErrorMessage } from '../ErrorMessage'
import { factorLabel, formatDateTime, formatMoney, statusLabel } from '../format'
import { allowedNextStatuses } from './status'

interface Props {
  identifier: string
  onBack: () => void
}

export function QuoteDetail({ identifier, onBack }: Props) {
  const api = useApi()
  const loadQuote = useCallback(() => api.getQuote(identifier), [api, identifier])
  const { data: quote, error, loading, setData } = useLoad(loadQuote)

  return (
    <section>
      <button type="button" className="link" onClick={onBack}>
        ← Back to quotes
      </button>

      <ErrorMessage error={error} />
      {loading && <p className="muted">Loading quote…</p>}
      {quote && <QuoteView quote={quote} onUpdated={setData} />}
    </section>
  )
}

function QuoteView({ quote, onUpdated }: { quote: QuoteDto; onUpdated: (quote: QuoteDto) => void }) {
  const factors = Object.entries(quote.ratingFactors)

  return (
    <>
      <div className="toolbar">
        <h2>
          {quote.reference} <span className="status">{statusLabel(quote.status)}</span>
        </h2>
      </div>

      <div className="card">
        <p className="premium">
          {formatMoney(quote.monthlyPremium, quote.currency)} <span className="muted">per month</span>
        </p>
        <dl className="facts">
          <dt>Client</dt>
          <dd>{quote.client.fullName}</dd>
          <dt>Date of birth</dt>
          <dd>{quote.client.dateOfBirth}</dd>
          <dt>Smoker</dt>
          <dd>{quote.client.isSmoker ? 'Yes' : 'No'}</dd>
          <dt>Product</dt>
          <dd>{quote.product.code}</dd>
          <dt>Cover</dt>
          <dd>{formatMoney(quote.product.coverAmount, quote.currency)}</dd>
          <dt>Term</dt>
          <dd>{quote.product.termYears} years</dd>
          <dt>Pricing version</dt>
          <dd>{quote.pricingVersion}</dd>
          <dt>Created</dt>
          <dd>{formatDateTime(quote.createdAt)}</dd>
          {quote.notes && (
            <>
              <dt>Notes</dt>
              <dd>{quote.notes}</dd>
            </>
          )}
        </dl>
      </div>

      <StatusChange quote={quote} onUpdated={onUpdated} />

      <h3>Rating factors</h3>
      {factors.length === 0 ? (
        <p className="muted">None recorded.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Factor</th>
              <th className="number">Value</th>
            </tr>
          </thead>
          <tbody>
            {factors.map(([name, value]) => (
              <tr key={name}>
                <td>{factorLabel(name)}</td>
                <td className="number">{value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h3>Status history</h3>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>From</th>
              <th>To</th>
              <th>By</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>
            {quote.statusHistory.map((change, index) => (
              <tr key={index}>
                <td>{formatDateTime(change.changedAt)}</td>
                <td>{change.fromStatus ? statusLabel(change.fromStatus) : '–'}</td>
                <td>{statusLabel(change.toStatus)}</td>
                <td>{change.changedBy}</td>
                <td>{change.reason ?? ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function StatusChange({ quote, onUpdated }: { quote: QuoteDto; onUpdated: (quote: QuoteDto) => void }) {
  const api = useApi()
  const [reason, setReason] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const next = allowedNextStatuses(quote.status)

  async function changeTo(status: QuoteStatus) {
    setSaving(true)
    setError(null)
    try {
      onUpdated(await api.updateQuoteStatus(quote.identifier, { status, reason: reason.trim() || null }))
      setReason('')
    } catch (e) {
      setError(e)
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <h3>Change status</h3>
      <ErrorMessage error={error} />
      {next.length === 0 ? (
        <p className="muted">This quote is {statusLabel(quote.status).toLowerCase()}; its status is final.</p>
      ) : (
        <div className="form card">
          <label className="field">
            <span>Reason (optional)</span>
            <textarea rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
          </label>
          <div className="actions">
            {next.map((status) => (
              <button key={status} type="button" disabled={saving} onClick={() => changeTo(status)}>
                Mark as {statusLabel(status).toLowerCase()}
              </button>
            ))}
          </div>
        </div>
      )}
    </>
  )
}
