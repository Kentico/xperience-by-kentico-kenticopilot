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

/** Named colors that commonly appear in hand-written design CSS, in canonical rgb()/rgba() form. */
const NAMED_COLORS: Record<string, string> = {
  transparent: 'rgba(0, 0, 0, 0)',
  black: 'rgb(0, 0, 0)',
  white: 'rgb(255, 255, 255)',
  red: 'rgb(255, 0, 0)',
  green: 'rgb(0, 128, 0)',
  blue: 'rgb(0, 0, 255)',
};

/**
 * Normalize a CSS color to canonical rgb()/rgba() form. getComputedStyle
 * already returns rgb()/rgba() in Chromium, so this mostly canonicalizes
 * spacing and folds alpha-1 rgba into rgb.
 */
function normalizeColor(value: string): string {
  if (!value) return value;
  const v = value.trim().toLowerCase();
  if (NAMED_COLORS[v]) return NAMED_COLORS[v];
  const m = v.match(/^rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)$/);
  if (m) {
    const [, r, g, b, a] = m;
    if (a === undefined || Number(a) === 1) return `rgb(${Number(r)}, ${Number(g)}, ${Number(b)})`;
    return `rgba(${Number(r)}, ${Number(g)}, ${Number(b)}, ${Number(a)})`;
  }
  const hex = v.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/);
  if (hex) {
    let h = hex[1];
    if (h.length === 3) h = h.split('').map((c) => c + c).join('');
    const n = parseInt(h, 16);
    return `rgb(${(n >> 16) & 255}, ${(n >> 8) & 255}, ${n & 255})`;
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
  if (property === 'font-weight') return (WEIGHT_KEYWORDS[a.trim().toLowerCase()] ?? a.trim().toLowerCase()) === (WEIGHT_KEYWORDS[b.trim().toLowerCase()] ?? b.trim().toLowerCase());
  if (property === 'font-family') return primaryFontFamily(a) === primaryFontFamily(b);
  const pa = parsePx(a);
  const pb = parsePx(b);
  if (pa !== null && pb !== null) return Math.abs(pa - pb) <= pxTolerance;
  // border-radius / box-shadow can hold several lengths: compare piecewise.
  const partsA = a.split(/\s+/);
  const partsB = b.split(/\s+/);
  if (partsA.length === partsB.length && partsA.length > 1) {
    return partsA.every((p, i) => styleValuesEqual(property, p, partsB[i], pxTolerance));
  }
  return normalizeText(a).toLowerCase() === normalizeText(b).toLowerCase();
}
