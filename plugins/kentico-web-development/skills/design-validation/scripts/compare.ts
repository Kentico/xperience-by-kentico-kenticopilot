#!/usr/bin/env node
// design-validation — compare a live Xperience by Kentico page against a
// static HTML design and report differences with root-cause hints.
//
// CLI entry point: argument parsing, page orchestration, console summary,
// exit codes. The pipeline itself lives in src/ (see src/shared/types.ts for
// an overview of the data flow). Runs directly on Node.js 22.18+ via native
// TypeScript type stripping — no build step.
//
// Usage:
//   node compare.ts --design <file|folder> --live <url> [options]
//
// Run with --help for all options.

import { parseArgs } from 'node:util';
import { mkdir, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { serveStatic } from './src/extraction/static-server.ts';
import { launchBrowser, loadAndExtract } from './src/extraction/browser.ts';
import { discoverPages, servedUrlFor, liveUrlFor, type DesignPage } from './src/pages.ts';
import { buildSemanticTree } from './src/analysis/semantic-tree.ts';
import { matchTrees } from './src/analysis/match-trees.ts';
import { diffContent, languageFallbackSuspected } from './src/reporting/diff-content.ts';
import { diffStructure } from './src/reporting/diff-structure.ts';
import { diffStyle } from './src/reporting/diff-style.ts';
import { buildReport, SEVERITY_ORDER } from './src/reporting/report.ts';
import type { Severity } from './src/shared/types.ts';

/** Usage text printed by --help. */
const HELP = `design-validation — compare a live page against a static HTML design

Usage:
  node compare.ts --design <file|folder> --live <url> [options]

Options:
  --design <path>          Static design HTML file, or a folder — every *.html in the
                           folder is auto-paired with --live (index.html → the base URL,
                           about.html → <base>/about, sub/index.html → <base>/sub)
  --live <url>             Live page URL, or the site base URL when --design is a folder
                           (mind Xperience language prefixes: /<lang>/...)
  --only <names>           Comma-separated page names to run (folder-derived pages)
  --viewport <WxH>         Viewport, e.g. 1280x900 (default 1280x900)
  --language <code>        Expected live-page language, e.g. en, cs (detects fallback)
  --out <path>             Report output file (single page) or directory (batch)
  --ignore <selector>      CSS selector to exclude from comparison (repeatable)
  --style-tolerance <px>   Pixel tolerance for style comparisons (default 1)
  --fail-on <severity>     Exit code 2 when findings at/above severity exist (high|medium|low)
  --timeout <ms>           Per-page navigation timeout (default 30000)
  --help                   Show this help

Output: one JSON report per page + a console summary.
Exit codes: 0 ok, 1 usage/runtime error (incl. pages that failed to load), 2 --fail-on matched.`;

/** Print a usage error and exit with code 1. */
function fail(message: string): never {
  console.error(`Error: ${message}\n\nRun with --help for usage.`);
  process.exit(1);
}

/** Parse a WIDTHxHEIGHT viewport argument. */
function parseViewport(value: string): { width: number; height: number } {
  const m = value.match(/^(\d+)x(\d+)$/i);
  if (!m) fail(`Invalid --viewport '${value}' — expected WIDTHxHEIGHT, e.g. 1280x900.`);
  return { width: Number(m[1]), height: Number(m[2]) };
}

/** Print the per-page console summary. */
function printSummary(report: ReturnType<typeof buildReport>, outPath: string): void {
  const { summary, findings } = report;
  const comparison = report.comparison as any;
  const lines = [''];
  lines.push(`Design vs live: ${comparison.name}`);
  lines.push(`  design: ${comparison.design.source}`);
  lines.push(`  live:   ${comparison.live.url} (HTTP ${comparison.live.httpStatus ?? '?'})`);
  if (comparison.language.fallbackSuspected) {
    lines.push(
      `  ! language fallback suspected: requested '${comparison.language.requested}', got '${comparison.language.detected}'`,
    );
  }
  lines.push('');
  lines.push(`  ${summary.headline}`);
  lines.push(
    `  content: ${summary.totals.content}  structure: ${summary.totals.structure}  style: ${summary.totals.style}` +
      `  |  high: ${summary.bySeverity.high}  medium: ${summary.bySeverity.medium}  low: ${summary.bySeverity.low}`,
  );
  lines.push('');
  for (const f of findings.slice(0, 15)) {
    lines.push(`  [${f.id}] (${f.severity}, likely ${f.classificationHint}) ${f.title}`);
    if (f.live?.selector) lines.push(`         live:   ${f.live.selector}`);
    if (f.design?.selector) lines.push(`         design: ${f.design.selector}`);
  }
  if (findings.length > 15) lines.push(`  … and ${findings.length - 15} more — see the JSON report.`);
  lines.push('');
  lines.push(`  Full report: ${outPath}`);
  console.log(lines.join('\n'));
}

/** Parse arguments, compare every page, write reports, and set the exit code. */
async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      design: { type: 'string' },
      live: { type: 'string' },
      only: { type: 'string' },
      viewport: { type: 'string' },
      language: { type: 'string' },
      out: { type: 'string' },
      ignore: { type: 'string', multiple: true },
      'style-tolerance': { type: 'string' },
      'fail-on': { type: 'string' },
      timeout: { type: 'string' },
      help: { type: 'boolean' },
    },
  });

  if (values.help) {
    console.log(HELP);
    process.exit(0);
  }
  if (!values.design || !values.live) {
    fail('Provide both --design <file|folder> and --live <url>.');
  }

  const viewport = values.viewport ? parseViewport(values.viewport) : { width: 1280, height: 900 };
  const language = values.language ?? null;
  const ignoreSelectors = values.ignore ?? [];
  const pxTolerance = values['style-tolerance'] !== undefined ? Number(values['style-tolerance']) : 1;
  if (Number.isNaN(pxTolerance)) fail('--style-tolerance must be a number.');
  const timeout = values.timeout !== undefined ? Number(values.timeout) : 30000;
  if (Number.isNaN(timeout)) fail('--timeout must be a number of milliseconds.');
  const failOn = values['fail-on'];
  if (failOn && !(failOn in SEVERITY_ORDER)) fail('--fail-on must be one of: high, medium, low.');

  // Design side: a file or a folder of pages, always served locally.
  const designAbs = path.resolve(values.design);
  const designInfo = await stat(designAbs).catch(() => null);
  if (!designInfo) fail(`Design path '${designAbs}' does not exist.`);
  const designRoot = designInfo.isDirectory() ? designAbs : path.dirname(designAbs);

  let pages: DesignPage[];
  if (designInfo.isDirectory()) {
    pages = await discoverPages(designAbs);
    if (pages.length === 0) fail(`Design folder '${designAbs}' contains no .html files.`);
  } else {
    pages = [{ name: path.basename(designAbs, path.extname(designAbs)), rel: path.basename(designAbs) }];
  }

  if (values.only) {
    const only = new Set(values.only.split(',').map((s) => s.trim()).filter(Boolean));
    for (const name of only) {
      if (!pages.some((p) => p.name === name)) fail(`--only: no page named '${name}'.`);
    }
    pages = pages.filter((p) => only.has(p.name));
  }

  if (!/^https?:\/\//i.test(values.live)) fail(`--live '${values.live}' is not an http(s) URL.`);

  const multipleReports = pages.length > 1;
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const scriptDir = path.dirname(fileURLToPath(import.meta.url));
  const outDir = values.out ? path.resolve(values.out) : path.join(scriptDir, 'reports');

  const designServer = await serveStatic(designRoot);
  const browser = await launchBrowser();
  let worstSeverity: Severity | null = null;
  const failed: { name: string; error: string }[] = [];
  try {
    for (const page of pages) {
      const designUrl = servedUrlFor(designServer.baseUrl, page.rel);
      const liveUrl = liveUrlFor(values.live, page.name);
      console.log(`Comparing '${page.name}': ${page.rel} vs ${liveUrl} @ ${viewport.width}x${viewport.height}`);

      let report;
      try {
        const loadOptions = { viewport, ignoreSelectors, timeout, language };
        const designResult = await loadAndExtract(browser, designUrl, loadOptions);
        const liveResult = await loadAndExtract(browser, liveUrl, loadOptions);
        // An error page is not a comparison target — fail the page, not the batch.
        if (liveResult.httpStatus !== null && liveResult.httpStatus >= 400) {
          throw new Error(`Live page returned HTTP ${liveResult.httpStatus} for ${liveUrl}`);
        }

        const designTree = buildSemanticTree(designResult.extraction);
        const liveTree = buildSemanticTree(liveResult.extraction);
        const match = matchTrees(designTree, liveTree);

        const findings = [
          ...diffContent(match, { liveMeta: liveTree.meta, requestedLanguage: language }),
          ...diffStructure(match),
          ...diffStyle(match, { pxTolerance }),
        ];

        const detectedLang = liveTree.meta.lang ?? null;
        report = buildReport({
          comparison: {
            name: page.name,
            design: {
              source: path.join(designRoot, ...page.rel.split('/')),
              servedUrl: designUrl,
              title: designTree.meta.title ?? undefined,
              lang: designTree.meta.lang ?? undefined,
            },
            live: {
              url: liveUrl,
              title: liveTree.meta.title ?? undefined,
              lang: detectedLang ?? undefined,
              httpStatus: liveResult.httpStatus,
            },
            viewport,
            language: {
              requested: language,
              detected: detectedLang,
              fallbackSuspected: Boolean(language && detectedLang && languageFallbackSuspected(language, detectedLang)),
            },
            ignoreSelectors,
          },
          findings,
        });
      } catch (err) {
        // One page failing (404, timeout) must not abort the batch.
        const message = (err as Error).message;
        console.error(`  x '${page.name}' failed: ${message}`);
        failed.push({ name: page.name, error: message });
        continue;
      }

      const outPath =
        values.out && !multipleReports && path.extname(values.out).toLowerCase() === '.json'
          ? path.resolve(values.out)
          : path.join(outDir, `${page.name.replace(/[\\/]/g, '_')}.${timestamp}.json`);
      await mkdir(path.dirname(outPath), { recursive: true });
      await writeFile(outPath, JSON.stringify(report, null, 2), 'utf8');
      printSummary(report, outPath);

      for (const f of report.findings) {
        if (worstSeverity === null || SEVERITY_ORDER[f.severity] < SEVERITY_ORDER[worstSeverity]) {
          worstSeverity = f.severity;
        }
      }
    }
  } finally {
    await browser.close();
    await designServer.close();
  }

  if (failed.length > 0) {
    console.error(`\n${failed.length} page(s) failed to compare:`);
    for (const f of failed) console.error(`  x ${f.name}: ${f.error}`);
    process.exit(1);
  }

  if (failOn && worstSeverity !== null && SEVERITY_ORDER[worstSeverity] <= SEVERITY_ORDER[failOn]) {
    process.exit(2);
  }
}

main().catch((err: Error) => {
  console.error(`Error: ${err.message}`);
  process.exit(1);
});
