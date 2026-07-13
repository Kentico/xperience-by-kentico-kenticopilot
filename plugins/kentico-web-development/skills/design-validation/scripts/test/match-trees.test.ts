// Regression tests for landmark-mismatch handling: a design without ARIA
// landmarks (everything under the synthetic 'body' landmark) must still have
// its content compared against a live page that uses real landmarks.
// Runs without a browser: node --test test/

import assert from 'node:assert/strict';
import { test } from 'node:test';

import { matchTrees } from '../src/analysis/match-trees.ts';
import { diffContent } from '../src/reporting/diff-content.ts';
import { diffStructure } from '../src/reporting/diff-structure.ts';
import type { Block, Landmark, Leaf, PageMeta, SemanticTree } from '../src/shared/types.ts';

const META: PageMeta = { lang: 'en', title: 'Test', url: 'http://test/' };

function textLeaf(text: string, headingLevel: number | null = null): Leaf {
  return {
    type: 'text',
    text,
    attrs: {},
    styles: {},
    headingLevel,
    locator: { selector: `p:${text.slice(0, 10)}`, snippet: text },
  };
}

function block(texts: string[], headingText?: string): Block {
  const leaves = [
    ...(headingText ? [textLeaf(headingText, 2)] : []),
    ...texts.map((t) => textLeaf(t)),
  ];
  return {
    tag: 'div',
    role: null,
    classes: [],
    styles: {},
    heading: headingText ?? null,
    headingLevel: headingText ? 2 : null,
    aggText: leaves.map((l) => l.text).join(' '),
    leaves,
    locator: { selector: 'div', snippet: leaves[0]?.text ?? '' },
  };
}

function landmark(role: string, blocks: Block[]): Landmark {
  return { role, blocks, locator: { selector: role, snippet: '' } };
}

function tree(landmarks: Landmark[]): SemanticTree {
  return { meta: META, landmarks };
}

const CONTENT_CONTEXT = { liveMeta: { lang: 'en' }, requestedLanguage: null };

/** Design without landmarks: one synthetic 'body' landmark holding all blocks. */
function landmarklessDesign(headerNav: string, heroHeading: string, footerText: string): SemanticTree {
  return tree([
    landmark('body', [
      block([headerNav], 'Site header'),
      block(['Welcome to our services and solutions'], heroHeading),
      block([footerText], 'Footer'),
    ]),
  ]);
}

/** Live page with real landmarks carrying the same three blocks. */
function landmarkedLive(headerNav: string, heroHeading: string, footerText: string): SemanticTree {
  return tree([
    landmark('banner', [block([headerNav], 'Site header')]),
    landmark('main', [block(['Welcome to our services and solutions'], heroHeading)]),
    landmark('contentinfo', [block([footerText], 'Footer')]),
  ]);
}

test('identical content matches across mismatched landmark structure', () => {
  const design = landmarklessDesign('Home About Contact', 'Great products', '+420 111 222 333');
  const live = landmarkedLive('Home About Contact', 'Great products', '+420 111 222 333');
  const match = matchTrees(design, live);

  assert.equal(match.blockPairs.length, 3, 'all blocks should pair up');
  assert.equal(match.unmatchedDesignBlocks.length, 0);
  assert.equal(match.unmatchedLiveBlocks.length, 0);
  assert.equal(diffContent(match, CONTENT_CONTEXT).length, 0, 'no content findings');
});

test('content differences are detected despite mismatched landmark structure', () => {
  const design = landmarklessDesign('Home About Contact', 'Great products', '+420 111 222 333');
  const live = landmarkedLive('Home About Careers', 'Great products', '+420 999 888 777');
  const match = matchTrees(design, live);

  const findings = diffContent(match, CONTENT_CONTEXT);
  const mismatchTexts = findings.filter((f) => f.kind === 'text-mismatch').map((f) => f.expected);
  assert.ok(mismatchTexts.includes('Home About Contact'), 'wrong nav label must be reported');
  assert.ok(mismatchTexts.includes('+420 111 222 333'), 'wrong footer phone must be reported');
});

test("synthetic 'body' landmark produces no landmark or moved-block findings", () => {
  const design = landmarklessDesign('Home About Contact', 'Great products', '+420 111 222 333');
  const live = landmarkedLive('Home About Contact', 'Great products', '+420 111 222 333');
  const findings = diffStructure(matchTrees(design, live));

  assert.ok(
    !findings.some((f) => f.kind === 'missing-landmark' && f.expected === 'body'),
    "'body' must not be reported as a missing landmark",
  );
  assert.ok(
    !findings.some((f) => f.expected === 'body' || f.actual === 'body'),
    "moves in/out of the synthetic 'body' landmark must not be reported",
  );
});

test('a block truly absent from the live page is still reported missing', () => {
  const design = tree([
    landmark('body', [
      block(['Welcome to our services and solutions'], 'Great products'),
      block(['This section only exists in the design'], 'Design-only section'),
    ]),
  ]);
  const live = tree([
    landmark('main', [block(['Welcome to our services and solutions'], 'Great products')]),
  ]);
  const match = matchTrees(design, live);

  assert.equal(match.unmatchedDesignBlocks.length, 1);
  const findings = diffStructure(match);
  assert.ok(
    findings.some((f) => f.kind === 'missing-block' && f.expected === 'Design-only section'),
    'the design-only block must be reported as missing',
  );
});

test('a genuinely moved block between real landmarks is still flagged', () => {
  const design = tree([
    landmark('banner', [block(['Home About'], 'Site header')]),
    landmark('main', [block(['Special offer for all customers'], 'Promo banner')]),
  ]);
  const live = tree([
    landmark('banner', [
      block(['Home About'], 'Site header'),
      block(['Special offer for all customers'], 'Promo banner'),
    ]),
    landmark('main', []),
  ]);
  const findings = diffStructure(matchTrees(design, live));
  assert.ok(
    findings.some((f) => f.expected === 'main' && f.actual === 'banner'),
    'block moved from main to banner must be reported',
  );
});
