// Style diff: compares the curated computed-style set on MATCHED element
// pairs only — unmatched elements never produce style findings, so missing
// content does not masquerade as styling noise.

import { snippet } from '../shared/text.ts';
import { styleValuesEqual } from '../shared/style-values.ts';
import type { Finding, Locator, MatchResult, Severity, StyleDelta } from '../shared/types.ts';

/** Properties whose delta makes a style finding high severity. */
const HIGH_IMPACT = new Set(['color', 'background-color', 'font-family', 'display']);
/** Properties whose delta makes a style finding medium severity; everything else is low. */
const MEDIUM_IMPACT = new Set([
  'font-size', 'font-weight', 'text-align', 'text-transform',
  'flex-direction', 'justify-content', 'align-items', 'grid-template-columns',
]);

/** Changed properties between two computed-style sets, honoring px tolerance. */
function compareStyles(
  designStyles: Record<string, string>,
  liveStyles: Record<string, string> | undefined,
  pxTolerance: number,
): StyleDelta[] {
  const changed: StyleDelta[] = [];
  // border-color/width compute even with no border (color follows
  // currentColor) — only meaningful when a border is actually drawn.
  const borderless =
    designStyles['border-top-style'] === 'none' && liveStyles?.['border-top-style'] === 'none';
  for (const property of Object.keys(designStyles)) {
    if (borderless && (property === 'border-top-color' || property === 'border-top-width')) continue;
    const expected = designStyles[property];
    const actual = liveStyles?.[property];
    if (actual === undefined) continue;
    if (!styleValuesEqual(property, expected, actual, pxTolerance)) {
      changed.push({ property, expected, actual });
    }
  }
  return changed;
}

/** Severity from the highest-impact changed property. */
function styleSeverity(changedProperties: StyleDelta[]): Severity {
  if (changedProperties.some((c) => HIGH_IMPACT.has(c.property))) return 'high';
  if (changedProperties.some((c) => MEDIUM_IMPACT.has(c.property))) return 'medium';
  return 'low';
}

/** Build one style-delta finding for a matched element pair. */
function makeStyleFinding(
  designLocator: Locator,
  liveLocator: Locator,
  label: string,
  changedProperties: StyleDelta[],
): Finding {
  return {
    category: 'style',
    kind: 'style-delta',
    severity: styleSeverity(changedProperties),
    title: `Styles differ on ${label}: ${changedProperties.map((c) => c.property).join(', ')}`,
    design: { ...designLocator, value: null },
    live: { ...liveLocator, value: null },
    expected: changedProperties.map((c) => `${c.property}: ${c.expected}`).join('; '),
    actual: changedProperties.map((c) => `${c.property}: ${c.actual}`).join('; '),
    details: { kind: 'style-delta', changedProperties },
  };
}

/** Style diff pass: computed-style deltas on matched leaves and block roots. */
export function diffStyle(match: MatchResult, { pxTolerance = 1 }: { pxTolerance?: number } = {}): Finding[] {
  const findings: Finding[] = [];
  const seen = new Set<string>(); // dedupe by live selector — leaves can repeat (link + text)

  for (const pair of match.leafPairs) {
    // Rescued pairs were matched despite low similarity; their styles often
    // belong to genuinely different elements — compare only solid matches.
    if (pair.rescued) continue;
    const key = pair.live.locator.selector;
    if (seen.has(key)) continue;
    const changed = compareStyles(pair.design.styles, pair.live.styles, pxTolerance);
    if (changed.length > 0) {
      seen.add(key);
      const label =
        pair.type === 'image'
          ? `image "${snippet(pair.design.text || pair.design.attrs.src || '', 40)}"`
          : `${pair.type === 'link' ? 'link' : pair.design.headingLevel ? 'heading' : 'text'} "${snippet(pair.design.text, 40)}"`;
      findings.push(makeStyleFinding(pair.design.locator, pair.live.locator, label, changed));
    }
  }

  // Block roots: catches container-level deltas (background, padding, layout).
  for (const pair of match.blockPairs) {
    const key = pair.live.locator.selector;
    if (seen.has(key)) continue;
    const changed = compareStyles(pair.design.styles, pair.live.styles, pxTolerance);
    if (changed.length > 0) {
      seen.add(key);
      const label = pair.design.heading
        ? `block "${snippet(pair.design.heading, 40)}"`
        : `block <${pair.design.tag}>`;
      findings.push(makeStyleFinding(pair.design.locator, pair.live.locator, label, changed));
    }
  }

  return findings;
}
