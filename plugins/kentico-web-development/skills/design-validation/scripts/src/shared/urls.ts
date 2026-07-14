// URL helpers shared by the matcher and the content diff.

/** True for href/src values left unresolved by the server (Xperience '~/' virtual paths). */
export function isUnresolvedVirtualUrl(url: unknown): boolean {
  return typeof url === 'string' && /^~\//.test(url.trim());
}

/** Base origin used to parse relative URLs; its host marks "no host of its own". */
const RELATIVE_BASE = 'http://relative.invalid';

/** Explicit lowercased host of a parsed URL, or null when it came from RELATIVE_BASE. */
function explicitHost(u: URL): string | null {
  const host = u.host.toLowerCase();
  return host === 'relative.invalid' || host === '' ? null : host;
}

/**
 * Last path segment normalized for fuzzy comparison: lowercased, query/hash
 * dropped, '.html'/'.htm' stripped, and a final 'index' segment collapsed into
 * its parent (design folders use 'about.html'/'sub/index.html' where live
 * sites use extensionless routes like '/en/about' or '/sub').
 */
function normalizedBasename(u: URL): string {
  const segments = u.pathname.split('/').filter(Boolean);
  let last = (segments.pop() ?? '').toLowerCase().replace(/\.html?$/, '');
  if (last === 'index') last = (segments.pop() ?? '').toLowerCase();
  return last;
}

/**
 * Fuzzy "same target" comparison of two authored URLs. Different explicit
 * hosts never match (a facebook link is not a twitter link even though both
 * paths are '/'); otherwise the normalized basenames must agree, so
 * 'about.html' matches '/en/about' and 'index.html' matches '/'.
 */
export function sameUrlTarget(a: string | null | undefined, b: string | null | undefined): boolean {
  if (!a && !b) return true;
  if (!a || !b) return false;
  let ua: URL;
  let ub: URL;
  try {
    ua = new URL(a, RELATIVE_BASE);
    ub = new URL(b, RELATIVE_BASE);
  } catch {
    return a.trim() === b.trim();
  }
  const hostA = explicitHost(ua);
  const hostB = explicitHost(ub);
  if (hostA && hostB && hostA !== hostB) return false;
  return normalizedBasename(ua) === normalizedBasename(ub);
}
