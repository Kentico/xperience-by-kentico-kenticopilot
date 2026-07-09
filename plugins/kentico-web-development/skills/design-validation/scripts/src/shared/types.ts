// Shared types for the design-validation pipeline. No runtime code.
//
// Data flow: extraction/extract-page.ts produces RawNode trees in the browser,
// analysis/semantic-tree.ts normalizes them into landmarks → blocks → leaves,
// analysis/match-trees.ts pairs the design and live trees into a MatchResult,
// and the reporting/diff-*.ts passes turn the MatchResult into Findings.

/** Raw DOM node as produced by extractPage() in extraction/extract-page.ts. */
export interface RawNode {
  tag: string;
  role: string | null;
  id: string | null;
  classes: string[];
  selector: string;
  ownText: string;
  fullText?: string;
  attrs: Record<string, string>;
  headingLevel: number | null;
  styles: Record<string, string>;
  children: RawNode[];
}

/** Document metadata captured alongside the extracted tree. */
export interface PageMeta {
  lang: string | null;
  title: string | null;
  url: string;
}

/** Points a finding at a concrete element on one side of the comparison. */
export interface Locator {
  selector: string;
  snippet: string;
}

/** Discriminator for the three independently compared leaf channels. */
export type LeafType = 'text' | 'link' | 'image';

/** Atomic content unit compared between design and live. */
export interface Leaf {
  type: LeafType;
  text: string;
  attrs: Record<string, string>;
  styles: Record<string, string>;
  headingLevel: number | null;
  richText?: boolean;
  locator: Locator;
}

/** A content block (roughly: one widget / section worth of content). */
export interface Block {
  tag: string;
  role: string | null;
  classes: string[];
  styles: Record<string, string>;
  heading: string | null;
  headingLevel: number | null;
  aggText: string;
  leaves: Leaf[];
  locator: Locator;
}

/** Top-level page area identified by ARIA landmark role. */
export interface Landmark {
  role: string;
  blocks: Block[];
  locator: Locator;
}

/** Normalized page: ordered landmarks derived from an extracted RawNode tree. */
export interface SemanticTree {
  meta: PageMeta;
  landmarks: Landmark[];
}

/** A design block matched to a live block. */
export interface BlockPair {
  design: Block;
  live: Block;
  score: number;
  landmarkRole: string;
  movedFromLandmark?: { design: string; live: string };
}

/** A design leaf matched to a live leaf of the same type, inside a matched block pair. */
export interface LeafPair {
  design: Leaf;
  live: Leaf;
  type: LeafType;
  score: number;
  rescued: boolean;
  blockPair: BlockPair;
}

/** Complete outcome of matching a design tree against a live tree. */
export interface MatchResult {
  blockPairs: BlockPair[];
  unmatchedDesignBlocks: { block: Block; landmarkRole: string }[];
  unmatchedLiveBlocks: { block: Block; landmarkRole: string }[];
  unmatchedDesignLandmarks: Landmark[];
  unmatchedLiveLandmarks: Landmark[];
  leafPairs: LeafPair[];
  unmatchedDesignLeaves: { leaf: Leaf; blockPair: BlockPair }[];
  unmatchedLiveLeaves: { leaf: Leaf; blockPair: BlockPair }[];
}

/** Finding severity, ordered high → low. */
export type Severity = 'high' | 'medium' | 'low';

/** One changed computed-style property on a matched element pair. */
export interface StyleDelta {
  property: string;
  expected: string;
  actual: string;
}

/** Locator plus the concrete value observed on that side of the comparison. */
export interface FindingLocation extends Locator {
  value: string | null | undefined;
}

/** One difference between design and live, as emitted by the diff passes. */
export interface Finding {
  category: 'content' | 'structure' | 'style';
  kind: string;
  severity: Severity;
  title: string;
  design: FindingLocation | null;
  live: FindingLocation | null;
  expected: string | null | undefined;
  actual: string | null | undefined;
  details: Record<string, unknown>;
}
