// Design-folder page discovery and design↔live URL pairing.

import { readdir } from 'node:fs/promises';
import path from 'node:path';

/** Folders skipped during discovery — design partials, not pages with a live URL. */
const SKIP_DIRS = new Set(['node_modules', 'partials', 'components', 'fragments']);

/** One discovered design page: display name and path relative to the design root. */
export interface DesignPage {
  name: string;
  rel: string;
}

/** Recursively discover *.html pages under a design folder. */
export async function discoverPages(root: string): Promise<DesignPage[]> {
  const out: DesignPage[] = [];
  async function walk(dir: string): Promise<void> {
    for (const entry of await readdir(dir, { withFileTypes: true })) {
      const abs = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (SKIP_DIRS.has(entry.name.toLowerCase())) continue;
        await walk(abs);
      } else if (entry.isFile() && /\.html?$/i.test(entry.name) && !entry.name.startsWith('_')) {
        const rel = path.relative(root, abs).split(path.sep).join('/');
        // about.html → about; about/index.html → about; index.html → index
        let name = rel.replace(/\.html?$/i, '');
        if (name !== 'index') name = name.replace(/\/index$/i, '');
        out.push({ name, rel });
      }
    }
  }
  await walk(root);
  out.sort((a, b) => a.name.localeCompare(b.name));
  return out;
}

/** URL of a file relative to a static-server base. */
export function servedUrlFor(base: string, rel: string): string {
  return `${base}/${rel.split('/').map(encodeURIComponent).join('/')}`;
}

/** Live URL for a folder-derived page name: index → base, about → base/about. */
export function liveUrlFor(base: string, name: string): string {
  const trimmed = base.replace(/\/+$/, '');
  return name === 'index' ? trimmed : `${trimmed}/${name}`;
}
