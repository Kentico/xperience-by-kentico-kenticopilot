// The curated computed-style property set and value-level comparison rules
// (color canonicalization, font shorthand quirks, px tolerance).

import { normalizeText } from './text.ts';

/** Curated computed-style properties compared by the style diff. */
export const STYLE_PROPERTIES = [
  // typography
  'font-family',
  'font-size',
  'font-weight',
  'font-style',
  'line-height',
  'letter-spacing',
  'text-transform',
  'text-align',
  'text-decoration-line',
  // color
  'color',
  'background-color',
  // spacing
  'margin-top',
  'margin-right',
  'margin-bottom',
  'margin-left',
  'padding-top',
  'padding-right',
  'padding-bottom',
  'padding-left',
  // layout
  'display',
  'flex-direction',
  'justify-content',
  'align-items',
  'gap',
  'grid-template-columns',
  // box
  'border-top-width',
  'border-top-style',
  'border-top-color',
  'border-radius',
  'box-shadow',
];

/**
 * Normalize a CSS color to canonical rgb()/rgba() form. Both sides come from
 * getComputedStyle, which already returns rgb()/rgba() in Chromium, so this
 * only canonicalizes spacing and folds alpha-1 rgba into rgb.
 */
function normalizeColor(value: string): string {
  if (!value) return value;
  const v = value.trim().toLowerCase();
  const m = v.match(/^rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)$/);
  if (m) {
    const [, r, g, b, a] = m;
    if (a === undefined || Number(a) === 1) return `rgb(${Number(r)}, ${Number(g)}, ${Number(b)})`;
    return `rgba(${Number(r)}, ${Number(g)}, ${Number(b)}, ${Number(a)})`;
  }
  return v;
}

/** font-weight keywords mapped to their numeric equivalents. */
const WEIGHT_KEYWORDS: Record<string, string> = { normal: '400', bold: '700' };

/**
 * Normalize a font-family list: strip quotes, collapse spacing, lowercase,
 * and keep only the FIRST family — fallback tails routinely differ between a
 * hand-written design stylesheet and a generated live one while the rendered
 * font is identical, so they are pure noise.
 */
function primaryFontFamily(value: string): string {
  if (!value) return value;
  const families = value
    .split(',')
    .map((f) => f.trim().replace(/^["']|["']$/g, '').toLowerCase())
    .filter(Boolean);
  return families[0] ?? '';
}

/** Parse a px length; returns null for non-px values (auto, %, keywords). */
function parsePx(value: unknown): number | null {
  if (typeof value !== 'string') return null;
  const m = value.trim().match(/^(-?[\d.]+)px$/);
  return m ? Number(m[1]) : null;
}

/**
 * Compare two computed-style values for a property, honoring px tolerance.
 * Returns true when they should be considered EQUAL.
 */
export function styleValuesEqual(
  property: string,
  a: string | null | undefined,
  b: string | null | undefined,
  pxTolerance = 1,
): boolean {
  if (a === b) return true;
  if (a == null || b == null) return false;
  if (/color/i.test(property)) return normalizeColor(a) === normalizeColor(b);
  // 'letter-spacing: normal' computes to the keyword but means 0.
  if (property === 'letter-spacing') {
    const norm = (v: string) => (v.trim().toLowerCase() === 'normal' ? '0px' : v);
    a = norm(a);
    b = norm(b);
    if (a === b) return true;
  }
  if (property === 'font-weight') return (WEIGHT_KEYWORDS[a.trim().toLowerCase()] ?? a.trim().toLowerCase()) === (WEIGHT_KEYWORDS[b.trim().toLowerCase()] ?? b.trim().toLowerCase());
  if (property === 'font-family') return primaryFontFamily(a) === primaryFontFamily(b);
  const pa = parsePx(a);
  const pb = parsePx(b);
  if (pa !== null && pb !== null) return Math.abs(pa - pb) <= pxTolerance;
  // border-radius / box-shadow can hold several parts: compare piecewise.
  // The split is paren-aware so 'rgba(0, 0, 0, 0.5)' stays one token.
  const partsA = a.split(/\s+(?![^(]*\))/);
  const partsB = b.split(/\s+(?![^(]*\))/);
  if (partsA.length === partsB.length && partsA.length > 1) {
    return partsA.every((p, i) => styleValuesEqual(property, p, partsB[i], pxTolerance));
  }
  return normalizeText(a).toLowerCase() === normalizeText(b).toLowerCase();
}
