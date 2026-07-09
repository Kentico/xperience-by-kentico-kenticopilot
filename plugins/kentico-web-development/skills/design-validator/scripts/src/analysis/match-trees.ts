// Fuzzy matching of the design semantic tree against the live one:
// landmarks by ARIA role, blocks by heading/text similarity and position,
// leaves by content similarity per type (text / link / image).

import { textSimilarity } from '../shared/text.ts';
import { urlBasename } from '../shared/urls.ts';
import type { Block, Landmark, Leaf, MatchResult, SemanticTree } from '../shared/types.ts';

/** Minimum blockScore for two blocks to be considered the same block. */
const BLOCK_MATCH_FLOOR = 0.4;
/** Minimum leafScore for two leaves to be considered the same element. */
const LEAF_MATCH_FLOOR = 0.35;

/** Score similarity of two blocks in [0, 1]. */
function blockScore(
  design: Block,
  live: Block,
  designIndex: number,
  liveIndex: number,
  designCount: number,
  liveCount: number,
): number {
  const positionSim =
    1 - Math.abs(designIndex / Math.max(designCount - 1, 1) - liveIndex / Math.max(liveCount - 1, 1));
  const tagAgreement = design.tag === live.tag || design.role === live.role ? 1 : 0;
  const textSim = textSimilarity(design.aggText, live.aggText);

  if (design.heading && live.heading) {
    const headingSim = textSimilarity(design.heading, live.heading);
    return 0.5 * headingSim + 0.15 * tagAgreement + 0.1 * positionSim + 0.25 * textSim;
  }
  if (design.heading || live.heading) {
    // Only one side has a heading — rely on text but penalize slightly.
    return 0.9 * (0.2 * tagAgreement + 0.15 * positionSim + 0.65 * textSim);
  }
  return 0.2 * tagAgreement + 0.2 * positionSim + 0.6 * textSim;
}

/** Score similarity of two leaves of the same type in [0, 1]. */
function leafScore(
  design: Leaf,
  live: Leaf,
  designIndex: number,
  liveIndex: number,
  designCount: number,
  liveCount: number,
): number {
  const positionSim =
    1 - Math.abs(designIndex / Math.max(designCount - 1, 1) - liveIndex / Math.max(liveCount - 1, 1));
  switch (design.type) {
    case 'text': {
      const headingBonus =
        design.headingLevel && live.headingLevel ? 0.1 : design.headingLevel || live.headingLevel ? -0.1 : 0;
      return 0.8 * textSimilarity(design.text, live.text) + 0.2 * positionSim + headingBonus;
    }
    case 'link': {
      const hrefSim = urlBasename(design.attrs.href) === urlBasename(live.attrs.href) ? 1 : 0;
      return 0.45 * textSimilarity(design.text, live.text) + 0.35 * hrefSim + 0.2 * positionSim;
    }
    case 'image': {
      const srcSim = urlBasename(design.attrs.src) === urlBasename(live.attrs.src) ? 1 : 0;
      const altSim = textSimilarity(design.text, live.text);
      return 0.4 * altSim + 0.35 * srcSim + 0.25 * positionSim;
    }
    default:
      return 0;
  }
}

/** Similarity scorer: two items plus their positions and list sizes → [0, 1]. */
type ScoreFn<T> = (a: T, b: T, indexA: number, indexB: number, countA: number, countB: number) => number;

/** Outcome of greedyMatch: matched pairs plus the leftovers on each side. */
interface GreedyResult<T> {
  pairs: { a: T; b: T; score: number; rescued?: boolean }[];
  unmatchedA: T[];
  unmatchedB: T[];
}

/** Greedy best-first matching of two item lists with a similarity floor. */
function greedyMatch<T>(itemsA: T[], itemsB: T[], scoreFn: ScoreFn<T>, floor: number): GreedyResult<T> {
  const candidates: { i: number; j: number; score: number }[] = [];
  itemsA.forEach((a, i) => {
    itemsB.forEach((b, j) => {
      const score = scoreFn(a, b, i, j, itemsA.length, itemsB.length);
      if (score >= floor) candidates.push({ i, j, score });
    });
  });
  candidates.sort((x, y) => y.score - x.score);

  const usedA = new Set<number>();
  const usedB = new Set<number>();
  const pairs: GreedyResult<T>['pairs'] = [];
  for (const { i, j, score } of candidates) {
    if (usedA.has(i) || usedB.has(j)) continue;
    usedA.add(i);
    usedB.add(j);
    pairs.push({ a: itemsA[i], b: itemsB[j], score });
  }
  return {
    pairs,
    unmatchedA: itemsA.filter((_, i) => !usedA.has(i)),
    unmatchedB: itemsB.filter((_, j) => !usedB.has(j)),
  };
}

/**
 * Rescue pass: when exactly one item of a type is unmatched on each side,
 * pair them anyway — a heavily rewritten value is a mismatch finding, which is
 * far more useful than a missing+extra pair.
 */
function rescueSingletons<T>(result: GreedyResult<T>): GreedyResult<T> {
  if (result.unmatchedA.length === 1 && result.unmatchedB.length === 1) {
    result.pairs.push({ a: result.unmatchedA[0], b: result.unmatchedB[0], score: 0, rescued: true });
    result.unmatchedA = [];
    result.unmatchedB = [];
  }
  return result;
}

/** Match two semantic trees into a MatchResult (see shared/types.ts). */
export function matchTrees(designTree: SemanticTree, liveTree: SemanticTree): MatchResult {
  const result: MatchResult = {
    blockPairs: [],
    unmatchedDesignBlocks: [],
    unmatchedLiveBlocks: [],
    unmatchedDesignLandmarks: [],
    unmatchedLiveLandmarks: [],
    leafPairs: [],
    unmatchedDesignLeaves: [],
    unmatchedLiveLeaves: [],
  };

  // 1. Pair landmarks by role, in document order within each role.
  const roles = new Set([
    ...designTree.landmarks.map((l) => l.role),
    ...liveTree.landmarks.map((l) => l.role),
  ]);
  const landmarkPairs: { design: Landmark; live: Landmark; role: string }[] = [];
  for (const role of roles) {
    const designLandmarks = designTree.landmarks.filter((l) => l.role === role);
    const liveLandmarks = liveTree.landmarks.filter((l) => l.role === role);
    const count = Math.max(designLandmarks.length, liveLandmarks.length);
    for (let i = 0; i < count; i++) {
      if (designLandmarks[i] && liveLandmarks[i]) {
        landmarkPairs.push({ design: designLandmarks[i], live: liveLandmarks[i], role });
      } else if (designLandmarks[i]) {
        result.unmatchedDesignLandmarks.push(designLandmarks[i]);
      } else {
        result.unmatchedLiveLandmarks.push(liveLandmarks[i]);
      }
    }
  }

  // 2. Match blocks inside each landmark pair.
  const blockMatchResults: { m: GreedyResult<Block>; role: string }[] = [];
  for (const { design, live, role } of landmarkPairs) {
    const m = greedyMatch(design.blocks, live.blocks, blockScore, BLOCK_MATCH_FLOOR);
    blockMatchResults.push({ m, role });
  }

  // 2b. Cross-landmark rescue: a block "missing" from its landmark may have
  // been rendered under a different landmark on the live site (e.g. content
  // placed outside <main>). Try unmatched design blocks against unmatched
  // live blocks from ALL landmarks before declaring them missing.
  const allUnmatchedDesign = blockMatchResults.flatMap(({ m, role }) =>
    m.unmatchedA.map((block) => ({ block, role })));
  const allUnmatchedLive = blockMatchResults.flatMap(({ m, role }) =>
    m.unmatchedB.map((block) => ({ block, role })));
  const cross = greedyMatch(
    allUnmatchedDesign,
    allUnmatchedLive,
    (a, b, i, j, ca, cb) => blockScore(a.block, b.block, i, j, ca, cb),
    BLOCK_MATCH_FLOOR + 0.15, // stricter: different landmark placement needs strong content evidence
  );

  for (const { m, role } of blockMatchResults) {
    for (const { a, b, score } of m.pairs) {
      result.blockPairs.push({ design: a, live: b, score, landmarkRole: role });
    }
  }
  for (const { a, b, score } of cross.pairs) {
    result.blockPairs.push({
      design: a.block,
      live: b.block,
      score,
      landmarkRole: a.role,
      movedFromLandmark: a.role !== b.role ? { design: a.role, live: b.role } : undefined,
    });
  }
  result.unmatchedDesignBlocks = cross.unmatchedA.map(({ block, role }) => ({ block, landmarkRole: role }));
  result.unmatchedLiveBlocks = cross.unmatchedB.map(({ block, role }) => ({ block, landmarkRole: role }));

  // 3. Match leaves within each matched block pair, per leaf type.
  for (const blockPair of result.blockPairs) {
    for (const type of ['text', 'link', 'image'] as const) {
      const a = blockPair.design.leaves.filter((l) => l.type === type);
      const b = blockPair.live.leaves.filter((l) => l.type === type);
      const m = rescueSingletons(greedyMatch(a, b, leafScore, LEAF_MATCH_FLOOR));
      for (const pair of m.pairs) {
        result.leafPairs.push({ design: pair.a, live: pair.b, type, score: pair.score, rescued: !!pair.rescued, blockPair });
      }
      for (const leaf of m.unmatchedA) result.unmatchedDesignLeaves.push({ leaf, blockPair });
      for (const leaf of m.unmatchedB) result.unmatchedLiveLeaves.push({ leaf, blockPair });
    }
  }

  return result;
}
