import { useCallback, useState, type FormEvent } from 'react'
import { useApi } from '../api/ApiContext'
import { errorDetails, type CreateQuoteResult } from '../api/client'
import { useIdempotencyKey } from '../api/idempotency'
import type { CreateQuoteRequest, ErrorDetail, ProductDto } from '../api/types'
import { useLoad } from '../api/useLoad'
import { ErrorMessage } from '../ErrorMessage'
import { formatMoney } from '../format'

interface FormState {
  productCode: string
  fullName: string
  dateOfBirth: string
  isSmoker: boolean
  coverAmount: string
  termYears: string
  notes: string
}

type FieldName = Exclude<keyof FormState, 'isSmoker'>

const emptyForm: FormState = {
  productCode: '',
  fullName: '',
  dateOfBirth: '',
  isSmoker: false,
  coverAmount: '',
  termYears: '',
  notes: '',
}

// Server field paths (FluentValidation property names) for the inputs that can show an error inline.
const serverFields: Record<FieldName, string> = {
  productCode: 'product.code',
  fullName: 'client.fullname',
  dateOfBirth: 'client.dateofbirth',
  coverAmount: 'product.coveramount',
  termYears: 'product.termyears',
  notes: 'notes',
}

function toRequest(form: FormState, productCode: string): CreateQuoteRequest {
  return {
    client: { fullName: form.fullName.trim(), dateOfBirth: form.dateOfBirth, isSmoker: form.isSmoker },
    product: { code: productCode, coverAmount: Number(form.coverAmount), termYears: Number(form.termYears) },
    notes: form.notes.trim() || null,
  }
}

function fieldOf(detail: ErrorDetail): FieldName | undefined {
  const path = detail.field?.toLowerCase()
  return (Object.keys(serverFields) as FieldName[]).find((name) => serverFields[name] === path)
}

interface Props {
  onOpen: (identifier: string) => void
  onCancel: () => void
}

export function CreateQuote({ onOpen, onCancel }: Props) {
  const api = useApi()
  const loadProducts = useCallback(() => api.getProducts(), [api])
  const products = useLoad(loadProducts)

  const [form, setForm] = useState<FormState>(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [result, setResult] = useState<CreateQuoteResult | null>(null)
  const idempotency = useIdempotencyKey()

  const productCode = form.productCode || products.data?.[0]?.code || ''
  const product = products.data?.find((p) => p.code === productCode)

  const details = errorDetails(error)
  const inlineErrors = (name: FieldName) => details.filter((d) => fieldOf(d) === name)
  const otherErrors = details.filter((d) => !fieldOf(d))

  const update = <K extends keyof FormState>(name: K, value: FormState[K]) =>
    setForm((current) => ({ ...current, [name]: value }))

  async function submit(event: FormEvent) {
    event.preventDefault()
    const request = toRequest(form, productCode)
    const key = idempotency.keyFor(request)

    setSubmitting(true)
    setError(null)
    try {
      const created = await api.createQuote(request, key)
      idempotency.reset()
      setResult(created)
    } catch (e) {
      setError(e)
    } finally {
      setSubmitting(false)
    }
  }

  if (result) {
    return (
      <section>
        <h2>New quote</h2>
        {result.replayed ? (
          <div className="notice" role="status">
            <p>
              This matches a quote you already submitted, so no new quote was created. Showing{' '}
              {result.quote.reference}.
            </p>
          </div>
        ) : (
          <div className="notice" role="status">
            <p>
              Quote {result.quote.reference} created: {formatMoney(result.quote.monthlyPremium, result.quote.currency)}{' '}
              per month.
            </p>
          </div>
        )}
        <div className="actions">
          <button type="button" onClick={() => onOpen(result.quote.identifier)}>
            View quote
          </button>
          <button
            type="button"
            className="secondary"
            onClick={() => {
              setResult(null)
              setForm(emptyForm)
            }}
          >
            Create another
          </button>
        </div>
      </section>
    )
  }

  return (
    <section>
      <h2>New quote</h2>
      <ErrorMessage error={products.error} />
      <ErrorMessage error={error} details={otherErrors} />

      <form className="form card" onSubmit={submit} noValidate>
        <label className="field">
          <span>Product</span>
          <select
            value={productCode}
            onChange={(e) => update('productCode', e.target.value)}
            disabled={products.loading}
          >
            {products.loading && <option value="">Loading products…</option>}
            {products.data?.map((p) => (
              <option key={p.code} value={p.code}>
                {p.name}
              </option>
            ))}
          </select>
          {product && <ProductLimits product={product} />}
          <FieldErrors errors={inlineErrors('productCode')} />
        </label>

        <label className="field">
          <span>Client full name</span>
          <input value={form.fullName} onChange={(e) => update('fullName', e.target.value)} maxLength={200} />
          <FieldErrors errors={inlineErrors('fullName')} />
        </label>

        <label className="field">
          <span>Date of birth</span>
          <input type="date" value={form.dateOfBirth} onChange={(e) => update('dateOfBirth', e.target.value)} />
          <FieldErrors errors={inlineErrors('dateOfBirth')} />
        </label>

        <label className="field inline">
          <input type="checkbox" checked={form.isSmoker} onChange={(e) => update('isSmoker', e.target.checked)} />
          <span>Smoker</span>
        </label>

        <label className="field">
          <span>Cover amount</span>
          <input
            type="number"
            inputMode="decimal"
            min={0}
            step={1000}
            value={form.coverAmount}
            onChange={(e) => update('coverAmount', e.target.value)}
          />
          <FieldErrors errors={inlineErrors('coverAmount')} />
        </label>

        <label className="field">
          <span>Term (years)</span>
          <input
            type="number"
            inputMode="numeric"
            min={1}
            value={form.termYears}
            onChange={(e) => update('termYears', e.target.value)}
          />
          <FieldErrors errors={inlineErrors('termYears')} />
        </label>

        <label className="field">
          <span>Notes (optional)</span>
          <textarea rows={3} maxLength={2000} value={form.notes} onChange={(e) => update('notes', e.target.value)} />
          <FieldErrors errors={inlineErrors('notes')} />
        </label>

        <div className="actions">
          <button type="submit" disabled={submitting || !productCode}>
            {submitting ? 'Creating…' : 'Create quote'}
          </button>
          <button type="button" className="secondary" onClick={onCancel}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}

function ProductLimits({ product }: { product: ProductDto }) {
  const amount = (value: number) => value.toLocaleString('en-ZA')
  return (
    <span className="hint">
      Cover {amount(product.minCoverAmount)} to {amount(product.maxCoverAmount)}, term{' '}
      {product.minTermYears} to {product.maxTermYears} years, entry age {product.minEntryAge} to {product.maxEntryAge}.
    </span>
  )
}

function FieldErrors({ errors }: { errors: ErrorDetail[] }) {
  return errors.map((e, index) => (
    <span key={index} className="field-error">
      {e.message}
    </span>
  ))
}
