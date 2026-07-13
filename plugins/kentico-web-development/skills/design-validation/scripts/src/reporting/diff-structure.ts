// Structure diff: missing/extra blocks and landmarks, heading-hierarchy
// mismatches, blocks rendered under a different landmark.

import { snippet } from '../shared/text.ts';
import type { Block, Finding, MatchResult } from '../shared/types.ts';

/**
 * The synthetic 'body' landmark (buildSemanticTree's container of last resort
 * for content outside any real landmark) is not authored markup — it can never
 * legitimately be "missing", and moves in/out of it are landmark-structure
 * noise, not content misplacement.
 */
const SYNTHETIC_ROLE = 'body';

/** Human-readable block identifier for finding titles. */
function blockTitle(block: Block): string {
  return block.heading ? `"${snippet(block.heading, 50)}"` : `<${block.tag}> "${snippet(block.aggText, 50)}"`;
}

/**
 * Structure diff pass: missing/extra landmarks and blocks, blocks rendered
 * under a different landmark, and heading-level mismatches.
 */
export function diffStructure(match: MatchResult): Finding[] {
  const findings: Finding[] = [];

  for (const landmark of match.unmatchedDesignLandmarks) {
    if (landmark.role === SYNTHETIC_ROLE) continue;
    findings.push({
      category: 'structure',
      kind: 'missing-landmark',
      severity: 'high',
      title: `Landmark '${landmark.role}' from the design is missing on the live page`,
      design: { ...landmark.locator, value: landmark.role },
      live: null,
      expected: landmark.role,
      actual: null,
      details: { kind: 'missing-landmark' },
    });
  }

  for (const landmark of match.unmatchedLiveLandmarks) {
    if (landmark.role === SYNTHETIC_ROLE) continue;
    findings.push({
      category: 'structure',
      kind: 'extra-landmark',
      severity: 'medium',
      title: `Landmark '${landmark.role}' on the live page is not in the design`,
      design: null,
      live: { ...landmark.locator, value: landmark.role },
      expected: null,
      actual: landmark.role,
      details: { kind: 'extra-landmark' },
    });
  }

  for (const { block, landmarkRole } of match.unmatchedDesignBlocks) {
    findings.push({
      category: 'structure',
      kind: 'missing-block',
      severity: 'high',
      title: `Block ${blockTitle(block)} from the design (${landmarkRole}) is missing on the live page`,
      design: { ...block.locator, value: block.heading ?? block.aggText },
      live: null,
      expected: block.heading ?? block.aggText,
      actual: null,
      details: { kind: 'missing-block' },
    });
  }

  for (const { block, landmarkRole } of match.unmatchedLiveBlocks) {
    findings.push({
      category: 'structure',
      kind: 'extra-block',
      severity: 'medium',
      title: `Block ${blockTitle(block)} on the live page (${landmarkRole}) is not in the design`,
      design: null,
      live: { ...block.locator, value: block.heading ?? block.aggText },
      expected: null,
      actual: block.heading ?? block.aggText,
      details: { kind: 'extra-block' },
    });
  }

  // Blocks matched across different landmarks (content placed in the wrong area).
  for (const pair of match.blockPairs) {
    if (
      pair.movedFromLandmark &&
      pair.movedFromLandmark.design !== SYNTHETIC_ROLE &&
      pair.movedFromLandmark.live !== SYNTHETIC_ROLE
    ) {
      findings.push({
        category: 'structure',
        kind: 'extra-block',
        severity: 'medium',
        title: `Block ${blockTitle(pair.design)} is under '${pair.movedFromLandmark.live}' on the live page but under '${pair.movedFromLandmark.design}' in the design`,
        design: { ...pair.design.locator, value: pair.movedFromLandmark.design },
        live: { ...pair.live.locator, value: pair.movedFromLandmark.live },
        expected: pair.movedFromLandmark.design,
        actual: pair.movedFromLandmark.live,
        details: { kind: 'extra-block' },
      });
    }
  }

  // Heading-level mismatches on matched text leaves.
  for (const pair of match.leafPairs) {
    if (
      pair.type === 'text' &&
      pair.design.headingLevel &&
      pair.live.headingLevel &&
      pair.design.headingLevel !== pair.live.headingLevel
    ) {
      findings.push({
        category: 'structure',
        kind: 'heading-hierarchy',
        severity: 'low',
        title: `Heading level differs for "${snippet(pair.design.text, 40)}": h${pair.design.headingLevel} in design, h${pair.live.headingLevel} live`,
        design: { ...pair.design.locator, value: `h${pair.design.headingLevel}` },
        live: { ...pair.live.locator, value: `h${pair.live.headingLevel}` },
        expected: `h${pair.design.headingLevel}`,
        actual: `h${pair.live.headingLevel}`,
        details: { kind: 'heading-hierarchy' },
      });
    }
  }

  return findings;
}
