// URL helpers shared by the matcher and the content diff.

/** True for href/src values left unresolved by the server (Xperience '~/' virtual paths). */
export function isUnresolvedVirtualUrl(url: unknown): boolean {
  return typeof url === 'string' && /^~\//.test(url.trim());
}

/** Last path segment of a URL, for fuzzy URL comparison. */
export function urlBasename(url: string | null | undefined): string {
  if (!url) return '';
  const path = url.replace(/^[a-z]+:\/\/[^/]+/i, '').split(/[?#]/)[0];
  const segments = path.split('/').filter(Boolean);
  return (segments[segments.length - 1] ?? '').toLowerCase();
}
