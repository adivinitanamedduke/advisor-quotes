import { expect, test, type Page } from '@playwright/test'

// A fresh adviser per run, so the tests never depend on what is already in the database
const adviserId = () => `E2E-${Date.now()}`

async function signInAs(page: Page, adviser: string) {
  await page.goto('/')
  await page.getByPlaceholder('e.g. ADV-001').fill(adviser)
  await page.keyboard.press('Enter')
  await expect(page.getByRole('heading', { name: 'Quotes', exact: true })).toBeVisible()
}

async function fillQuote(page: Page) {
  await page.getByLabel('Product').selectOption({ label: 'Term Life Cover' })
  await page.getByLabel('Client full name').fill('Thandi Mokoena')
  await page.getByLabel('Date of birth').fill('1985-03-14')
  await page.getByLabel('Smoker').check()
  await page.getByLabel('Cover amount').fill('1000000')
  await page.getByLabel('Term (years)').fill('20')
  await page.getByLabel('Notes (optional)').fill('Prefers email')
}

test('adviser creates a quote, resubmits it and takes it to accepted', async ({ page }) => {
  await signInAs(page, adviserId())
  await expect(page.getByText('No quotes found.')).toBeVisible()

  // Create: the server prices it (age 41 smoker, R1m over 20 years)
  await page.getByRole('button', { name: 'New quote' }).click()
  await fillQuote(page)
  await page.getByRole('button', { name: 'Create quote' }).click()
  const created = page.getByRole('status')
  await expect(created).toContainText(/Quote QT-\d{6} created/)
  // en-ZA money: "R 2 103,75" (non-breaking spaces, decimal comma)
  await expect(created).toContainText(/2\s?103[.,]75/)
  const reference = (await created.textContent())!.match(/QT-\d{6}/)![0]

  // Same submission again: the API recognises the duplicate and returns the original quote
  await page.getByRole('button', { name: 'Create another' }).click()
  await fillQuote(page)
  await page.getByRole('button', { name: 'Create quote' }).click()
  await expect(page.getByRole('status')).toContainText('This matches a quote you already submitted')
  await expect(page.getByRole('status')).toContainText(reference)

  // Detail: premium, rating factors and the initial history row
  await page.getByRole('button', { name: 'View quote' }).click()
  await expect(page.getByRole('heading', { name: new RegExp(`${reference}\\s+Draft`) })).toBeVisible()
  await expect(page.getByRole('cell', { name: 'Smoker loading' })).toBeVisible()

  // Draft → Sent to client → Accepted, each with a reason in the history
  await page.getByLabel('Reason (optional)').fill('Emailed to client')
  await page.getByRole('button', { name: 'Mark as sent to client' }).click()
  await expect(page.getByRole('heading', { name: new RegExp(`${reference}\\s+Sent to client`) })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Mark as draft' })).toHaveCount(0)

  await page.getByLabel('Reason (optional)').fill('Client signed')
  await page.getByRole('button', { name: 'Mark as accepted' }).click()
  await expect(page.getByRole('heading', { name: new RegExp(`${reference}\\s+Accepted`) })).toBeVisible()
  await expect(page.getByText('its status is final')).toBeVisible()
  const history = page.locator('table').last().locator('tbody tr')
  await expect(history).toHaveCount(3)
  await expect(history.last()).toContainText('Client signed')

  // Back on the list, filtered to accepted quotes
  await page.getByRole('button', { name: '← Back to quotes' }).click()
  await page.getByLabel('Status').selectOption('Accepted')
  await expect(page.getByRole('button', { name: reference })).toBeVisible()
})

test('server validation errors are shown with a trace id', async ({ page }) => {
  await signInAs(page, adviserId())

  // Too young for the product and no name: the API rejects it and the form shows why
  await page.getByRole('button', { name: 'New quote' }).click()
  await page.getByLabel('Product').selectOption({ label: 'Term Life Cover' })
  await page.getByLabel('Date of birth').fill('2015-01-01')
  await page.getByLabel('Cover amount').fill('1000000')
  await page.getByLabel('Term (years)').fill('20')
  await page.getByRole('button', { name: 'Create quote' }).click()

  const alert = page.getByRole('alert')
  await expect(alert).toBeVisible()
  await expect(alert).toContainText('Client full name is required')
  await expect(alert).toContainText(/Trace id: [0-9a-f]{32}/)
})
