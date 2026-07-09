// Root-cause classification hints and report assembly.
//
// Classification hints:
//   content — wrong/missing content item, field value, or translation in Xperience
//   serving — how content is rendered: widget/section/template/view-component
//   styling — CSS
// The hint is a starting point for the agent; the skill's classification
// playbooks make the final call.

import type { Finding } from '../shared/types.ts';

/** Sort order for severities — lower is more severe. */
export const SEVERITY_ORDER: Record<string, number> = { high: 0, medium: 1, low: 2 };

/** Per finding kind: the suggested root-cause class and what to check. */
const CLASSIFICATION_RULES: Record<string, { hint: string; reason: string }> = {
  'text-mismatch': {
    hint: 'content',
    reason:
      'The element matched between design and live; only its value differs. Verify the content item field (and its language variant) in Xperience.',
  },
  'missing-leaf': {
    hint: 'content',
    reason:
      'The surrounding block matched, but this element is absent — usually an empty content field or unlinked item. If the element is produced by its own widget, check serving instead.',
  },
  'extra-leaf': {
    hint: 'content',
    reason:
      'The live page renders an element the design does not have — often leftover or placeholder content; can also be a widget rendering extra markup.',
  },
  'language-fallback': {
    hint: 'content',
    reason:
      'The live page was served in a different language than requested. Xperience language fallback returns HTTP 200 with fallback content when a translation is missing — check the language variant of the page and its content items.',
  },
  'unresolved-url': {
    hint: 'serving',
    reason:
      "A literal '~/' virtual path reached the browser — the view renders the URL without resolving it. Add Url.Content/ResolveUrls (or asp-* tag helpers) in the widget or view that outputs this URL.",
  },
  'missing-block': {
    hint: 'serving',
    reason:
      'A whole block from the design is absent. Likely a missing widget in the Page Builder zone, a section/template not rendering, or a view component returning empty. If the widget exists but its data source is empty, the root cause is content.',
  },
  'extra-block': {
    hint: 'serving',
    reason:
      'The live page renders a block the design does not have (or in a different area) — check the page template, sections, and widget placement in Page Builder.',
  },
  'missing-landmark': {
    hint: 'serving',
    reason:
      'A top-level page area from the design is absent — check the layout view (_Layout.cshtml) and the page template.',
  },
  'extra-landmark': {
    hint: 'serving',
    reason:
      'The live page has a top-level area the design does not — check the layout view and the page template.',
  },
  'heading-hierarchy': {
    hint: 'serving',
    reason:
      'The text matches but the heading level differs — the view/widget renders the wrong heading tag.',
  },
  'style-delta': {
    hint: 'styling',
    reason:
      'Element and content matched; only computed CSS differs. Locate the stylesheet/SCSS partial responsible and align it with the design.',
  },
};

/** Tool identification embedded in every report. */
export const TOOL_INFO = { name: 'design-validator', version: '1.0.0' };

/**
 * Assemble the per-page JSON report: sort findings by severity, assign ids,
 * attach classification hints, compute summary counts and the headline.
 */
export function buildReport({ comparison, findings }: { comparison: unknown; findings: Finding[] }) {
  const sorted = [...findings].sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity]);
  const withIds = sorted.map((f, i) => {
    const { kind, ...rest } = f;
    const rule = CLASSIFICATION_RULES[kind] ?? {
      hint: 'unknown',
      reason: 'No classification rule matched; inspect both sides manually.',
    };
    return {
      id: `F${String(i + 1).padStart(3, '0')}`,
      ...rest,
      classificationHint: rule.hint,
      classificationReason: rule.reason,
      details: { ...(f.details ?? {}), kind },
    };
  });

  const totals: Record<string, number> = { findings: withIds.length, content: 0, structure: 0, style: 0 };
  const bySeverity: Record<string, number> = { high: 0, medium: 0, low: 0 };
  for (const f of withIds) {
    totals[f.category]++;
    bySeverity[f.severity]++;
  }

  const byHint: Record<string, number> = {};
  for (const f of withIds) byHint[f.classificationHint] = (byHint[f.classificationHint] ?? 0) + 1;
  const hintSummary = Object.entries(byHint)
    .map(([hint, count]) => `${count} likely ${hint}`)
    .join(', ');
  const headline =
    withIds.length === 0
      ? 'Live page matches the design — no differences found.'
      : `${withIds.length} difference${withIds.length === 1 ? '' : 's'} found (${hintSummary}).`;

  return {
    schemaVersion: '1.0',
    generatedAt: new Date().toISOString(),
    tool: TOOL_INFO,
    comparison,
    summary: { totals, bySeverity, headline },
    findings: withIds,
  };
}
