// Normalizes a raw extracted DOM tree into a semantic tree:
// landmarks → blocks → leaves.
//
// Live markup is developer-defined (Xperience widget/section wrappers are
// arbitrary), so positional DOM diffing is useless. The semantic tree strips
// wrapper noise and keeps only content-bearing structure, which
// match-trees.ts can then pair fuzzily.

import { normalizeText, snippet } from '../shared/text.ts';
import type { Block, Landmark, Leaf, Locator, RawNode, SemanticTree } from '../shared/types.ts';
import type { Extraction } from '../extraction/extract-page.ts';

/** ARIA roles treated as top-level page areas. */
export const LANDMARK_ROLES = new Set(['banner', 'navigation', 'main', 'complementary', 'contentinfo']);

/** Tags considered inline-level when deciding whether a node is a text container. */
const INLINE_TAGS = new Set([
  'span', 'strong', 'em', 'b', 'i', 'u', 'small', 'sub', 'sup', 'mark',
  'code', 'abbr', 'time', 'br', 'wbr', 'a', 'img', 'picture', 'source', 'label',
]);

/** Locator for a raw node: its CSS path plus a text snippet. */
function locatorOf(node: RawNode, text?: string): Locator {
  return { selector: node.selector, snippet: snippet(text ?? subtreeText(node)) };
}

/** Full normalized text of a raw subtree, in document order. */
function subtreeText(node: RawNode): string {
  // fullText is captured in-page from textContent, preserving document order.
  if (node.fullText !== undefined) return normalizeText(node.fullText);
  const parts = [node.ownText];
  for (const c of node.children) parts.push(subtreeText(c));
  return normalizeText(parts.filter(Boolean).join(' '));
}

/** True when every element in the subtree is inline-level (text container). */
function isTextContainer(node: RawNode): boolean {
  if (!subtreeText(node)) return false;
  const onlyInlineDescendants = (n: RawNode): boolean =>
    n.children.every((c) => INLINE_TAGS.has(c.tag) && onlyInlineDescendants(c));
  return onlyInlineDescendants(node);
}

/** True for Xperience rich-text wrappers (div.fr-view), whose interior is one content unit. */
function isRichText(node: RawNode): boolean {
  return node.classes.includes('fr-view');
}

/** Collapse chains of single-child wrappers with no own text. */
function collapseWrappers(node: RawNode): RawNode {
  let current = node;
  while (
    current.children.length === 1 &&
    !current.ownText &&
    !LANDMARK_ROLES.has(current.children[0].role ?? '') &&
    !isRichText(current)
  ) {
    current = current.children[0];
  }
  return current;
}

/**
 * Extract leaves from a raw subtree.
 * Leaf types are compared independently, so links/images nested in text
 * containers are emitted both as part of the text and as their own leaves:
 *  - text:  whole text containers (or own text of mixed nodes)
 *  - link:  every <a href>, with its text and authored href
 *  - image: every <img>, with alt and authored src
 */
function collectLeaves(node: RawNode, leaves: Leaf[] = []): Leaf[] {
  if (node.tag === 'img') {
    leaves.push({
      type: 'image',
      text: normalizeText(node.attrs.alt ?? ''),
      attrs: node.attrs,
      styles: node.styles,
      headingLevel: null,
      locator: locatorOf(node, node.attrs.alt ?? node.attrs.src ?? ''),
    });
    return leaves;
  }

  if (node.tag === 'a' && node.attrs.href !== undefined) {
    leaves.push({
      type: 'link',
      text: subtreeText(node),
      attrs: node.attrs,
      styles: node.styles,
      headingLevel: null,
      locator: locatorOf(node),
    });
    // Still collect images inside the link (e.g. linked card images).
    for (const c of node.children) collectImagesOnly(c, leaves);
    if (isTextContainer(node)) return leaves; // text covered by the link leaf itself
    // Block-level link wrapper (e.g. whole card wrapped in <a>): also collect
    // the text leaves inside so content diffs stay fine-grained.
    for (const c of node.children) collectTextLeaves(c, leaves);
    return leaves;
  }

  if (isRichText(node)) {
    leaves.push({
      type: 'text',
      text: subtreeText(node),
      attrs: {},
      styles: node.styles,
      headingLevel: null,
      richText: true,
      locator: locatorOf(node),
    });
    for (const c of node.children) collectLinksAndImages(c, leaves);
    return leaves;
  }

  if (isTextContainer(node)) {
    leaves.push({
      type: 'text',
      text: subtreeText(node),
      attrs: {},
      styles: node.styles,
      headingLevel: node.headingLevel,
      locator: locatorOf(node),
    });
    for (const c of node.children) collectLinksAndImages(c, leaves);
    return leaves;
  }

  // Mixed node: own text becomes a leaf, then recurse into children.
  if (node.ownText) {
    leaves.push({
      type: 'text',
      text: normalizeText(node.ownText),
      attrs: {},
      styles: node.styles,
      headingLevel: node.headingLevel,
      locator: locatorOf(node, node.ownText),
    });
  }
  for (const c of node.children) collectLeaves(c, leaves);
  return leaves;
}

/** Collect only the text leaves of a subtree (used under block-level link wrappers). */
function collectTextLeaves(node: RawNode, leaves: Leaf[]): void {
  const sub: Leaf[] = [];
  collectLeaves(node, sub);
  for (const leaf of sub) if (leaf.type === 'text') leaves.push(leaf);
}

/** Collect link and image leaves nested inside an already-emitted text leaf. */
function collectLinksAndImages(node: RawNode, leaves: Leaf[]): void {
  if (node.tag === 'a' && node.attrs.href !== undefined) {
    leaves.push({
      type: 'link',
      text: subtreeText(node),
      attrs: node.attrs,
      styles: node.styles,
      headingLevel: null,
      locator: locatorOf(node),
    });
  }
  if (node.tag === 'img') {
    leaves.push({
      type: 'image',
      text: normalizeText(node.attrs.alt ?? ''),
      attrs: node.attrs,
      styles: node.styles,
      headingLevel: null,
      locator: locatorOf(node, node.attrs.alt ?? node.attrs.src ?? ''),
    });
  }
  for (const c of node.children) collectLinksAndImages(c, leaves);
}

/** Collect only image leaves (used inside links, whose text is covered by the link leaf). */
function collectImagesOnly(node: RawNode, leaves: Leaf[]): void {
  if (node.tag === 'img') {
    leaves.push({
      type: 'image',
      text: normalizeText(node.attrs.alt ?? ''),
      attrs: node.attrs,
      styles: node.styles,
      headingLevel: null,
      locator: locatorOf(node, node.attrs.alt ?? node.attrs.src ?? ''),
    });
  }
  for (const c of node.children) collectImagesOnly(c, leaves);
}

/** Build a block descriptor from a raw node. */
function makeBlock(rawNode: RawNode): Block {
  const node = collapseWrappers(rawNode);
  const leaves = collectLeaves(node);
  const heading = leaves.find((l) => l.type === 'text' && l.headingLevel) ?? null;
  return {
    tag: node.tag,
    role: node.role,
    classes: node.classes,
    styles: node.styles,
    heading: heading ? heading.text : null,
    headingLevel: heading ? heading.headingLevel : null,
    aggText: leaves.filter((l) => l.type === 'text').map((l) => l.text).join(' '),
    leaves,
    locator: locatorOf(node),
  };
}

/** Build a landmark descriptor (role + blocks) from a raw landmark node. */
function makeLandmark(role: string, rawNode: RawNode): Landmark {
  const node = collapseWrappers(rawNode);
  const blocks = node.children.length > 0
    ? node.children.map((c) => makeBlock(c))
    : [makeBlock(node)]; // landmark with only direct content is its own block
  // Own text directly on the landmark element (rare) becomes a leading block.
  if (node.ownText && node.children.length > 0) {
    blocks.unshift(makeBlock({ ...node, children: [], fullText: node.ownText }));
  }
  return {
    role,
    blocks: blocks.filter((b) => b.leaves.length > 0 || b.aggText),
    locator: locatorOf(node),
  };
}

/**
 * Build the semantic tree: a flat ordered list of landmarks, each with blocks.
 * Content outside any landmark is grouped into an implicit 'body' landmark.
 */
export function buildSemanticTree(extraction: Extraction): SemanticTree {
  const landmarks: Landmark[] = [];
  const orphans: RawNode[] = [];

  /** True when the subtree contains a landmark at any depth. */
  function containsLandmark(node: RawNode): boolean {
    if (LANDMARK_ROLES.has(node.role ?? '')) return true;
    return node.children.some((c) => containsLandmark(c));
  }

  /** Collect landmarks in document order; non-landmark content becomes orphans. */
  function findLandmarks(node: RawNode): void {
    if (LANDMARK_ROLES.has(node.role ?? '')) {
      landmarks.push(makeLandmark(node.role as string, node));
      return;
    }
    if (node.children.some((c) => containsLandmark(c))) {
      // Mixed container: descend, keeping non-landmark children as orphans.
      if (node.ownText) orphans.push({ ...node, children: [], fullText: node.ownText });
      for (const c of node.children) findLandmarks(c);
    } else {
      orphans.push(node);
    }
  }

  if (extraction.tree) {
    for (const child of extraction.tree.children) findLandmarks(child);
    if (extraction.tree.ownText) {
      orphans.unshift({ ...extraction.tree, children: [], fullText: extraction.tree.ownText });
    }
  }

  if (orphans.length > 0) {
    const blocks = orphans.map((o) => makeBlock(o)).filter((b) => b.leaves.length > 0 || b.aggText);
    if (blocks.length > 0) {
      landmarks.push({
        role: 'body',
        blocks,
        locator: { selector: 'body', snippet: '' },
      });
    }
  }

  return { meta: extraction.meta, landmarks };
}
