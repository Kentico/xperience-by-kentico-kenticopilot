// Text normalization and fuzzy similarity scoring, shared by the semantic
// matcher and the diff passes.

/** Collapse whitespace, trim, Unicode-normalize. */
export function normalizeText(text: string | null | undefined): string {
  if (!text) return '';
  return text.normalize('NFC').replace(/\s+/g, ' ').trim();
}

/** Lowercased word tokens with punctuation stripped. */
function tokens(text: string): string[] {
  return normalizeText(text)
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s]/gu, ' ')
    .split(/\s+/)
    .filter(Boolean);
}

/** Token-set Jaccard similarity in [0, 1]. */
function jaccard(a: string, b: string): number {
  const ta = new Set(tokens(a));
  const tb = new Set(tokens(b));
  if (ta.size === 0 && tb.size === 0) return 1;
  if (ta.size === 0 || tb.size === 0) return 0;
  let inter = 0;
  for (const t of ta) if (tb.has(t)) inter++;
  return inter / (ta.size + tb.size - inter);
}

/** Levenshtein similarity ratio in [0, 1]. */
function levenshteinRatio(a: string, b: string): number {
  const s = normalizeText(a).toLowerCase();
  const t = normalizeText(b).toLowerCase();
  if (s === t) return 1;
  if (!s.length || !t.length) return 0;
  // Cap very long strings to keep matching fast; prefix is representative enough.
  const MAX = 400;
  const s2 = s.slice(0, MAX);
  const t2 = t.slice(0, MAX);
  let prev = Array.from({ length: t2.length + 1 }, (_, i) => i);
  let curr = new Array(t2.length + 1);
  for (let i = 1; i <= s2.length; i++) {
    curr[0] = i;
    for (let j = 1; j <= t2.length; j++) {
      const cost = s2[i - 1] === t2[j - 1] ? 0 : 1;
      curr[j] = Math.min(prev[j] + 1, curr[j - 1] + 1, prev[j - 1] + cost);
    }
    [prev, curr] = [curr, prev];
  }
  const dist = prev[t2.length];
  return 1 - dist / Math.max(s2.length, t2.length);
}

/** Combined text similarity: blends token overlap with edit distance. */
export function textSimilarity(a: string, b: string): number {
  const na = normalizeText(a);
  const nb = normalizeText(b);
  if (!na && !nb) return 1;
  if (!na || !nb) return 0;
  return 0.5 * jaccard(na, nb) + 0.5 * levenshteinRatio(na, nb);
}

/** Short excerpt of text for locators. */
export function snippet(text: string | null | undefined, max = 80): string {
  const t = normalizeText(text);
  return t.length <= max ? t : `${t.slice(0, max - 1)}…`;
}
