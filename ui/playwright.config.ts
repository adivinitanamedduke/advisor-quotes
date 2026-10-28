import { defineConfig } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5222'
const uiUrl = 'http://localhost:5173'

/**
 * Browser end-to-end tests against the real API and PostgreSQL.
 * Starts the API (needs ConnectionStrings__Quotes in the environment) and the Vite dev server,
 * drives the installed Chrome, and records a video of every test into test-results/.
 */
export default defineConfig({
  testDir: './e2e',
  outputDir: './test-results',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: uiUrl,
    channel: process.env.E2E_BROWSER_CHANNEL ?? 'chrome',
    viewport: { width: 1280, height: 800 },
    // Needs Playwright's ffmpeg (npx playwright install ffmpeg); E2E_VIDEO=off skips recording
    video: { mode: process.env.E2E_VIDEO === 'off' ? 'off' : 'on', size: { width: 1280, height: 800 } },
    trace: 'retain-on-failure',
    // Slow enough that each step is readable in the recording
    launchOptions: { slowMo: Number(process.env.E2E_SLOW_MO ?? 350) },
  },
  webServer: [
    {
      command: 'dotnet run --project ../API --no-launch-profile',
      url: `${apiUrl}/health/ready`,
      reuseExistingServer: true,
      timeout: 120_000,
      env: {
        ASPNETCORE_URLS: apiUrl,
        ASPNETCORE_ENVIRONMENT: 'Development',
      },
    },
    {
      command: 'npm run dev',
      url: uiUrl,
      reuseExistingServer: true,
      timeout: 60_000,
      env: { VITE_API_BASE_URL: apiUrl },
    },
  ],
})
