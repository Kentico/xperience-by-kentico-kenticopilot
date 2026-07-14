// Regression tests for buildSemanticTree: own-text-only blocks must not
// inherit the whole subtree's fullText, and childless elements (bare images)
// next to landmarks must survive into the synthetic 'body' landmark.
// Runs without a browser: node --test test/

import assert from 'node:assert/strict';
import { test } from 'node:test';

import { buildSemanticTree } from '../src/analysis/semantic-tree.ts';
import type { RawNode } from '../src/shared/types.ts';
import type { Extraction } from '../src/extraction/extract-page.ts';

/** RawNode factory mirroring the extractor: fullText is always set, in document order. */
function el(
  tag: string,
  opts: Partial<Pick<RawNode, 'role' | 'ownText' | 'attrs' | 'children'>> = {},
): RawNode {
  const children = opts.children ?? [];
  const ownText = opts.ownText ?? '';
  const fullText = [ownText, ...children.map((c) => c.fullText ?? '')]
    .filter(Boolean)
    .join(' ');
  return {
    tag,
    role: opts.role ?? null,
    id: null,
    classes: [],
    selector: tag,
    ownText,
    fullText,
    attrs: opts.attrs ?? {},
    headingLevel: null,
    styles: {},
    children,
  };
}

function extraction(body: RawNode): Extraction {
  return { meta: { lang: 'en', title: 'Test', url: 'http://test/' }, tree: body };
}

test("body direct text next to a landmark keeps only the body's own text", () => {
  // <body>© <main>Hello World</main></body>
  const body = el('body', {
    ownText: '©',
    children: [el('main', { role: 'main', children: [el('p', { ownText: 'Hello World' })] })],
  });
  const { landmarks } = buildSemanticTree(extraction(body));

  const bodyLandmark = landmarks.find((l) => l.role === 'body');
  assert.ok(bodyLandmark, "body own text must produce a synthetic 'body' landmark");
  assert.equal(bodyLandmark.blocks.length, 1);
  assert.equal(bodyLandmark.blocks[0].aggText, '©', 'orphan block must not swallow landmark content');
});

test("a container's own text does not duplicate its landmark children's content", () => {
  // <body><div>© <main>Hello World</main></div></body>
  const body = el('body', {
    children: [
      el('div', {
        ownText: '©',
        children: [el('main', { role: 'main', children: [el('p', { ownText: 'Hello World' })] })],
      }),
    ],
  });
  const { landmarks } = buildSemanticTree(extraction(body));

  const bodyLandmark = landmarks.find((l) => l.role === 'body');
  assert.ok(bodyLandmark);
  assert.equal(bodyLandmark.blocks[0].aggText, '©');
});

test("a landmark's own text becomes a leading block without its children's text", () => {
  // <main>Intro <div>Content</div></main>
  const body = el('body', {
    children: [
      el('main', {
        role: 'main',
        ownText: 'Intro',
        children: [el('div', { children: [el('p', { ownText: 'Content' })] })],
      }),
    ],
  });
  const { landmarks } = buildSemanticTree(extraction(body));

  const main = landmarks.find((l) => l.role === 'main');
  assert.ok(main);
  assert.equal(main.blocks[0].aggText, 'Intro', 'leading own-text block must not repeat child content');
  assert.ok(
    main.blocks.slice(1).some((b) => b.aggText === 'Content'),
    'child content must still be its own block',
  );
});

test('a bare image next to a landmark is kept as an image leaf', () => {
  // <body><header>…</header><img src="hero.png"></body>
  const body = el('body', {
    children: [
      el('header', { role: 'banner', children: [el('p', { ownText: 'Site header' })] }),
      el('img', { attrs: { src: 'hero.png', alt: 'Hero' } }),
    ],
  });
  const { landmarks } = buildSemanticTree(extraction(body));

  const bodyLandmark = landmarks.find((l) => l.role === 'body');
  assert.ok(bodyLandmark, 'the orphan image must produce a synthetic body landmark');
  assert.ok(
    bodyLandmark.blocks.some((b) => b.leaves.some((l) => l.type === 'image' && l.attrs.src === 'hero.png')),
    'the bare image must appear as an image leaf',
  );
});
