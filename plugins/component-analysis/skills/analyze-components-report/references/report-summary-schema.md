# Report summary schema

`analysis/component-analysis-summary.json` is intended as a machine-consumable handoff artifact for downstream agents and alternative presentation layers.

This file is produced by the analysis stage, not synthesized by the report stage.
The report stage validates artifact presence/schema compatibility and then copies SPA assets.

Use the following stable shape:

```json
{
  "schemaVersion": "1.0",
  "projectPath": "...",
  "outputRoot": "...",
  "generatedAtUtc": "...",
  "sourceArtifacts": {
    "analysisIndexPath": "analysis-index.json",
    "includedCategoryArtifacts": [
      {
        "category": "admin-ui",
        "path": "admin-ui.json",
        "coverageStatus": "analyzed"
      }
    ],
    "missingPrerequisites": []
  },
  "coverage": {
    "includedCategories": ["admin-ui", "page-builder"],
    "analyzedCategories": ["admin-ui"],
    "notPresentCategories": [],
    "notYetAnalyzedCategories": ["email-builder"],
    "unableToAssessCategories": []
  },
  "metrics": {
    "findingCount": 0,
    "recommendationCount": 0,
    "consistencyCheckCount": 0,
    "consistencyPassRate": null,
    "severityCounts": {
      "High": 0,
      "Medium": 0,
      "Low": 0
    },
    "aiRiskCounts": {
      "High": 0,
      "Medium": 0,
      "Low": 0
    }
  },
  "topRisks": [
    {
      "title": "...",
      "category": "admin-ui",
      "severity": "High",
      "aiRisk": "High",
      "confidence": "High",
      "falsePositiveRisk": "Low",
      "estimatedAgentEffort": "Medium",
      "evidence": ["..."]
    }
  ],
  "prioritizedActions": [
    {
      "id": "ACT-001",
      "summary": "...",
      "categories": ["admin-ui", "page-builder"],
      "sourceRecommendations": [
        {
          "category": "admin-ui",
          "recommendationIndex": 0
        }
      ],
      "priority": "P1|P2|P3",
      "remediationRisk": "Low|Medium|High",
      "estimatedAgentEffort": "Low|Medium|High",
      "confidence": "Low|Medium|High",
      "docReferences": ["..."],
      "linkedFinding": "...",
      "recommendedSteps": ["...", "..."]
    }
  ],
  "categorySnapshots": [
    {
      "category": "admin-ui",
      "coverageStatus": "analyzed",
      "overallAiRisk": "Medium",
      "summaryNotes": "...",
      "findingCount": 0,
      "recommendationCount": 0
    }
  ],
  "docsReferencesByCategory": {
    "admin-ui": ["..."],
    "page-builder": ["..."]
  }
}
```

## Determinism rules

- Use deterministic category ordering: `admin-ui`, `page-builder`, `email-builder`, `form-builder`, `global-extensibility`.
- Sort `topRisks` by: `severity` (High first), then `aiRisk` (High first), then `confidence` (High first), then `title` (A-Z).
- Sort `prioritizedActions` by: `priority` (`P1`, `P2`, `P3`), then `estimatedAgentEffort` (Low to High), then `summary` (A-Z).
- IDs in `prioritizedActions[].id` must be stable within one run and use zero-padded numbering (`ACT-001`, `ACT-002`, ...).
- Preserve explicit unknowns instead of fabrication. If a field cannot be reliably derived, use `null` when allowed, or omit the object from ranked arrays.

## Field semantics

### Rating fields (`Low|Medium|High`)

Apply these meanings consistently across all rating-like fields:

- `severity`: Impact magnitude if issue is real.
- `aiRisk`: Likelihood the issue causes AI-assisted implementation drift or unsafe edits.
- `confidence`: Confidence in the finding/action recommendation based on available evidence.
- `falsePositiveRisk`: Likelihood that the finding is not a real issue.
- `estimatedAgentEffort`: Relative implementation effort for a coding agent to remediate.
- `remediationRisk`: Likelihood that applying the proposed action causes regressions.

`confidence` and `falsePositiveRisk` are inversely related conceptually, but not strict mathematical inverses.

### Prioritized Actions structure

Each action in `prioritizedActions` includes:

- **`id`** (required): Unique action identifier using format `ACT-NNN` (zero-padded, 3 digits).
- **`summary`** (required): Concise description of the remediation action.
- **`categories`** (required): Array of category keys this action applies to.
- **`sourceRecommendations`** (required): Traceability links back to category-specific recommendations; ensures generated actions can be traced to their evidence.
- **`priority`** (required): `P1` (critical), `P2` (important), or `P3` (nice-to-have).
- **`remediationRisk`** (required): `Low|Medium|High` — likelihood the action causes regressions.
- **`estimatedAgentEffort`** (required): `Low|Medium|High` — relative effort for a coding agent.
- **`confidence`** (required): `Low|Medium|High` — confidence the action addresses the underlying issue.
- **`docReferences`** (required): Array of documentation URLs that provide context or implementation guidance.
- **`linkedFinding`** (optional): Title/identifier of the finding that prompted this action; provides reasoning context.
- **`recommendedSteps`** (optional): Array of step-by-step implementation guidance. When present, helps developers and agents understand _how_ to apply the action.

### Coverage classification

Coverage arrays in `coverage` represent mutually exclusive partitions for included categories:

- `analyzedCategories`: category analyzed with reliable results.
- `notPresentCategories`: category not implemented in the project.
- `unableToAssessCategories`: category exists but audit reliability is insufficient.
- `notYetAnalyzedCategories`: known category not included in this run.

`includedCategories` contains categories intentionally included in the run scope.

### topRisks ranking semantics

- `topRisks` should contain the highest-priority findings across included categories.
- Use deterministic ordering from the Determinism rules section.
- If required ranking fields are unknown, prefer excluding that item from ranked output rather than inventing values.

### prioritizedActions semantics

- `prioritizedActions` represents de-duplicated remediation actions across included categories.
- `sourceRecommendations` preserves traceability to source artifacts.
- `recommendationIndex` is zero-based and refers to the position in each source category file's `recommendations` array.

When merging similar actions, preserve all originating references in `sourceRecommendations` and all relevant categories in `categories`.

## Metric definition: consistencyPassRate

`metrics.consistencyPassRate` represents consistency-check pass coverage as a normalized fraction in the range `0..1`.

- Numerator: count of checks with `status = pass`.
- Denominator: count of checks with `status` in `pass|warning|fail`.
- Exclude checks with `status = not-applicable` from the denominator.
- If denominator is `0`, set `consistencyPassRate` to `null`.

Examples:

- 7 applicable checks, 5 pass -> `consistencyPassRate = 0.7142857143`
- 0 applicable checks -> `consistencyPassRate = null`

## Validation and failure behavior

Report-stage validation must fail fast and stop SPA report deployment when any required artifact is missing or invalid.

- Required artifacts: `analysis/analysis-index.json`, selected `analysis/*.json` category artifacts, `analysis/component-analysis-summary.json`.
- If validation fails, report the missing/invalid files and reasons in a concise error list.
- Do not run additional discovery, inference, or re-analysis in the report stage.
