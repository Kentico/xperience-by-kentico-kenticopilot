// Playwright Chromium helpers: launching with actionable install hints, and
// loading a page + running the in-page extractor.

import type { Browser } from 'playwright';

import { extractPage, type Extraction } from './extract-page.ts';
import { STYLE_PROPERTIES } from '../shared/style-values.ts';

/** Launch Playwright Chromium, printing an actionable setup hint (and exiting) when Playwright or the browser binary is missing. */
export async function launchBrowser(): Promise<Browser> {
  let chromium;
  try {
    ({ chromium } = await import('playwright'));
  } catch {
    console.error(
      'Playwright is not installed. Run setup first (from the scripts directory):\n' +
        '  npm ci && npx playwright install chromium',
    );
    process.exit(1);
  }
  try {
    return await chromium.launch();
  } catch (err) {
    if (/Executable doesn't exist/i.test((err as Error).message)) {
      console.error(
        'Chromium browser binary is missing. Run (from the scripts directory):\n' +
          '  npx playwright install chromium',
      );
      process.exit(1);
    }
    throw err;
  }
}

/** Page-load settings shared by both sides of a comparison. */
export interface LoadOptions {
  viewport: { width: number; height: number };
  ignoreSelectors?: string[];
  timeout?: number;
  language?: string | null;
}

/** Extraction plus the HTTP status of the initial navigation (null for non-HTTP responses). */
export interface LoadResult {
  extraction: Extraction;
  httpStatus: number | null;
}

/** Load a URL and extract its visible DOM. */
export async function loadAndExtract(
  browser: Browser,
  url: string,
  { viewport, ignoreSelectors = [], timeout = 30000, language }: LoadOptions,
): Promise<LoadResult> {
  const context = await browser.newContext({
    viewport,
    locale: language || undefined,
    // Local Xperience development runs on https://localhost with a self-signed certificate.
    ignoreHTTPSErrors: true,
  });
  const page = await context.newPage();
  try {
    const response = await page.goto(url, { waitUntil: 'load', timeout });
    const httpStatus = response?.status() ?? null;
    // Settle dynamic content; ignore timeout — 'load' already fired.
    await page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {});
    const extraction = await page.evaluate(extractPage, {
      ignoreSelectors,
      styleProperties: STYLE_PROPERTIES,
    });
    return { extraction, httpStatus };
  } catch (err) {
    throw new Error(`Failed to load and extract ${url}: ${(err as Error).message}`);
  } finally {
    await context.close();
  }
}
