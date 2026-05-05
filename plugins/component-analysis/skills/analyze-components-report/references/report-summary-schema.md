# Report summary schema

`reports/component-analysis-summary.json` is intended as a machine-consumable handoff artifact for downstream agents and alternative presentation layers.

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
        "path": "categories/admin-ui.json",
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
      "docReferences": ["..."]
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
