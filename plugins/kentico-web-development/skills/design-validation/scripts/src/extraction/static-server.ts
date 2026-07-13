// Minimal static file server, so design HTML loads with its relative
// CSS/asset links intact (and local "live" folders can be compared too).

import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';

/** Content-Type by file extension for the served design assets. */
const CONTENT_TYPES: Record<string, string> = {
  '.html': 'text/html; charset=utf-8',
  '.htm': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.webp': 'image/webp',
  '.avif': 'image/avif',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.otf': 'font/otf',
  '.mp4': 'video/mp4',
  '.webm': 'video/webm',
};

/** Handle to a running static server. */
export interface StaticServer {
  baseUrl: string;
  close: () => Promise<void>;
}

/** Serve `rootDir` statically on 127.0.0.1 with an ephemeral port. */
export async function serveStatic(rootDir: string): Promise<StaticServer> {
  const root = path.resolve(rootDir);

  const server = http.createServer(async (req, res) => {
    try {
      const urlPath = decodeURIComponent(new URL(req.url ?? '/', 'http://localhost').pathname);
      let filePath = path.resolve(root, '.' + urlPath);
      // Prevent path traversal outside the served root.
      const relative = path.relative(root, filePath);
      if (relative.startsWith('..') || path.isAbsolute(relative)) {
        res.writeHead(403).end('Forbidden');
        return;
      }
      let info = await stat(filePath).catch(() => null);
      if (info?.isDirectory()) {
        filePath = path.join(filePath, 'index.html');
        info = await stat(filePath).catch(() => null);
      }
      if (!info?.isFile()) {
        res.writeHead(404).end('Not found');
        return;
      }
      const ext = path.extname(filePath).toLowerCase();
      res.writeHead(200, {
        'Content-Type': CONTENT_TYPES[ext] ?? 'application/octet-stream',
        'Content-Length': info.size,
        'Cache-Control': 'no-store',
      });
      createReadStream(filePath).pipe(res);
    } catch (err) {
      res.writeHead(500).end(`Server error: ${(err as Error).message}`);
    }
  });

  await new Promise<void>((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => resolve());
  });

  const address = server.address();
  if (address === null || typeof address === 'string') throw new Error('Server has no port.');
  return {
    baseUrl: `http://127.0.0.1:${address.port}`,
    close: () => new Promise((resolve) => server.close(() => resolve())),
  };
}
