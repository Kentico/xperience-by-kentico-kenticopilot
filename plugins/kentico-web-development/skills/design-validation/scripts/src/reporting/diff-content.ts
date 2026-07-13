// Content diff: text mismatches, missing/extra leaves inside matched blocks,
// unresolved Xperience '~/' URLs, and language-fallback detection.

import { normalizeText, snippet } from '../shared/text.ts';
import { isUnresolvedVirtualUrl, urlBasename } from '../shared/urls.ts';
import type { Finding, Leaf, MatchResult } from '../shared/types.ts';

/** Human-readable leaf label for finding titles. */
function leafLabel(leaf: Leaf): string {
  if (leaf.type === 'image') return 'Image';
  if (leaf.type === 'link') return 'Link';
  if (leaf.headingLevel) return `Heading (h${leaf.headingLevel})`;
  if (leaf.richText) return 'Rich text';
  return 'Text';
}

/** Page-level context the content diff needs beyond the match itself. */
export interface ContentDiffContext {
  liveMeta: { lang: string | null };
  requestedLanguage: string | null;
}

/**
 * Content diff pass: value differences on matched leaves, missing/extra
 * leaves inside matched blocks, unresolved '~/' URLs, and language fallback.
 */
export function diffContent(match: MatchResult, { liveMeta, requestedLanguage }: ContentDiffContext): Finding[] {
  const findings: Finding[] = [];

  // Language fallback: Xperience serves HTTP 200 with fallback-language
  // content when a translation is missing — detect via <html lang>.
  if (requestedLanguage && liveMeta.lang && languageFallbackSuspected(requestedLanguage, liveMeta.lang)) {
    findings.push({
      category: 'content',
      kind: 'language-fallback',
      severity: 'high',
      title: `Live page is in '${liveMeta.lang}' but '${requestedLanguage}' was requested — language fallback suspected`,
      design: null,
      live: { selector: 'html', snippet: `lang="${liveMeta.lang}"`, value: liveMeta.lang },
      expected: requestedLanguage,
      actual: liveMeta.lang,
      details: { kind: 'language-fallback' },
    });
  }

  for (const pair of match.leafPairs) {
    if (pair.type === 'text') {
      if (normalizeText(pair.design.text) !== normalizeText(pair.live.text)) {
        findings.push({
          category: 'content',
          kind: 'text-mismatch',
          severity: pair.design.headingLevel ? 'high' : 'medium',
          title: `${leafLabel(pair.design)} differs: "${snippet(pair.design.text, 40)}" vs "${snippet(pair.live.text, 40)}"`,
          design: { ...pair.design.locator, value: pair.design.text },
          live: { ...pair.live.locator, value: pair.live.text },
          expected: pair.design.text,
          actual: pair.live.text,
          details: { kind: 'text-mismatch' },
        });
      }
    }

    if (pair.type === 'link') {
      const liveHref = pair.live.attrs.href;
      if (isUnresolvedVirtualUrl(liveHref)) {
        findings.push({
          category: 'content',
          kind: 'unresolved-url',
          severity: 'high',
          title: `Unresolved virtual URL in link href: "${liveHref}"`,
          design: { ...pair.design.locator, value: pair.design.attrs.href ?? null },
          live: { ...pair.live.locator, value: liveHref },
          expected: pair.design.attrs.href ?? null,
          actual: liveHref,
          details: { kind: 'unresolved-url', url: liveHref },
        });
      } else if (
        pair.design.attrs.href &&
        liveHref &&
        urlBasename(pair.design.attrs.href) !== urlBasename(liveHref)
      ) {
        findings.push({
          category: 'content',
          kind: 'text-mismatch',
          severity: 'medium',
          title: `Link "${snippet(pair.design.text, 40)}" points to a different target`,
          design: { ...pair.design.locator, value: pair.design.attrs.href },
          live: { ...pair.live.locator, value: liveHref },
          expected: pair.design.attrs.href,
          actual: liveHref,
          details: { kind: 'text-mismatch', url: liveHref },
        });
      }
      if (normalizeText(pair.design.text) !== normalizeText(pair.live.text)) {
        findings.push({
          category: 'content',
          kind: 'text-mismatch',
          severity: 'medium',
          title: `Link text differs: "${snippet(pair.design.text, 40)}" vs "${snippet(pair.live.text, 40)}"`,
          design: { ...pair.design.locator, value: pair.design.text },
          live: { ...pair.live.locator, value: pair.live.text },
          expected: pair.design.text,
          actual: pair.live.text,
          details: { kind: 'text-mismatch' },
        });
      }
    }

    if (pair.type === 'image') {
      const liveSrc = pair.live.attrs.src;
      if (isUnresolvedVirtualUrl(liveSrc)) {
        findings.push({
          category: 'content',
          kind: 'unresolved-url',
          severity: 'high',
          title: `Unresolved virtual URL in image src: "${liveSrc}"`,
          design: { ...pair.design.locator, value: pair.design.attrs.src ?? null },
          live: { ...pair.live.locator, value: liveSrc },
          expected: pair.design.attrs.src ?? null,
          actual: liveSrc,
          details: { kind: 'unresolved-url', url: liveSrc },
        });
      }
      if (normalizeText(pair.design.text) !== normalizeText(pair.live.text)) {
        findings.push({
          category: 'content',
          kind: 'text-mismatch',
          severity: 'low',
          title: `Image alt text differs: "${snippet(pair.design.text, 40)}" vs "${snippet(pair.live.text, 40)}"`,
          design: { ...pair.design.locator, value: pair.design.text },
          live: { ...pair.live.locator, value: pair.live.text },
          expected: pair.design.text,
          actual: pair.live.text,
          details: { kind: 'text-mismatch' },
        });
      }
    }
  }

  // Leaves present in the design but absent in an otherwise-matched live block.
  for (const { leaf, blockPair } of match.unmatchedDesignLeaves) {
    findings.push({
      category: 'content',
      kind: 'missing-leaf',
      severity: 'medium',
      title: `${leafLabel(leaf)} from the design is missing on the live page: "${snippet(leaf.text || leaf.attrs.src || '', 50)}"`,
      design: { ...leaf.locator, value: leaf.text || leaf.attrs.src || null },
      live: { ...blockPair.live.locator, value: null },
      expected: leaf.text || leaf.attrs.src || null,
      actual: null,
      details: { kind: 'missing-leaf' },
    });
  }

  // Leaves on the live page that the design does not have.
  for (const { leaf, blockPair } of match.unmatchedLiveLeaves) {
    findings.push({
      category: 'content',
      kind: 'extra-leaf',
      severity: 'low',
      title: `${leafLabel(leaf)} on the live page is not in the design: "${snippet(leaf.text || leaf.attrs.src || '', 50)}"`,
      design: { ...blockPair.design.locator, value: null },
      live: { ...leaf.locator, value: leaf.text || leaf.attrs.src || null },
      expected: null,
      actual: leaf.text || leaf.attrs.src || null,
      details: { kind: 'extra-leaf' },
    });

    // Unresolved URLs are worth flagging even on extra leaves.
    const url = leaf.attrs.href ?? leaf.attrs.src;
    if (isUnresolvedVirtualUrl(url)) {
      findings.push({
        category: 'content',
        kind: 'unresolved-url',
        severity: 'high',
        title: `Unresolved virtual URL on the live page: "${url}"`,
        design: null,
        live: { ...leaf.locator, value: url },
        expected: null,
        actual: url,
        details: { kind: 'unresolved-url', url },
      });
    }
  }

  // Unresolved URLs on the live page are a serving bug regardless of whether
  // the containing block matched anything in the design.
  for (const { block } of match.unmatchedLiveBlocks) {
    for (const leaf of block.leaves) {
      const url = leaf.attrs.href ?? leaf.attrs.src;
      if (isUnresolvedVirtualUrl(url)) {
        findings.push({
          category: 'content',
          kind: 'unresolved-url',
          severity: 'high',
          title: `Unresolved virtual URL on the live page: "${url}"`,
          design: null,
          live: { ...leaf.locator, value: url },
          expected: null,
          actual: url,
          details: { kind: 'unresolved-url', url },
        });
      }
    }
  }

  return findings;
}

/** True when the detected live-page language disagrees with the requested one. */
export function languageFallbackSuspected(requested: string, detected: string): boolean {
  const r = requested.toLowerCase();
  const d = detected.toLowerCase();
  return d !== r && !d.startsWith(`${r}-`) && !r.startsWith(`${d}-`);
}
