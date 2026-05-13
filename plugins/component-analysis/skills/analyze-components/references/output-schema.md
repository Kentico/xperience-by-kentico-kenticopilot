# Component analysis output schema

This document defines the human-readable contract for analysis artifacts.

For implementation, use both:

- JSON Schema files as the authoritative machine-validated contract.
- Example artifact JSON files as illustrative references for prompt authors and reviewers.

Recommended layout:

- `references/schemas/analysis-index.schema.json`
- `references/schemas/category-analysis.schema.json`
- `references/schemas/report-summary.schema.json`
- `references/examples/*.json`

When examples and schemas differ, the JSON Schema files are the source of truth.

Each category analysis file must use the following shared top-level structure:

```json
{
  "analysisMetadata": {
    "projectPath": "...",
    "outputRoot": "...",
    "category": "admin-ui|page-builder|email-builder|form-builder|global-extensibility",
    "generatedAtUtc": "...",
    "docsReferences": [
      {
        "title": "...",
        "url": "...",
        "source": "manifest|mcp-discovery"
      }
    ]
  },
  "coverage": {
    "status": "analyzed|not-present|partially-covered|unable-to-assess",
    "summary": "...",
    "analyzedComponentTypes": ["..."],
    "missingComponentTypes": ["..."]
  },
  "inventory": [
    {
      "componentType": "...",
      "name": "...",
      "identifier": "...",
      "registration": {
        "mechanism": "...",
        "location": "...",
        "metadata": {}
      },
      "files": ["..."],
      "relatedFiles": ["..."]
    }
  ],
  "consistencyChecks": [
    {
      "id": "...",
      "title": "...",
      "status": "pass|warning|fail|not-applicable",
      "importance": "high|medium|low",
      "details": "...",
      "evidence": ["..."],
      "docReferences": ["..."]
    }
  ],
  "findings": [
    {
      "title": "...",
      "severity": "Low|Medium|High",
      "aiRisk": "Low|Medium|High",
      "impact": "...",
      "whyItMatters": "...",
      "evidence": ["..."],
      "docReferences": ["..."],
      "confidence": "Low|Medium|High",
      "falsePositiveRisk": "Low|Medium|High",
      "estimatedAgentEffort": "Low|Medium|High"
    }
  ],
  "recommendations": [
    {
      "summary": "...",
      "targetPattern": "...",
      "migrationPath": ["..."],
      "remediationRisk": "Low|Medium|High",
      "estimatedAgentEffort": "Low|Medium|High",
      "confidence": "Low|Medium|High",
      "docReferences": ["..."]
    }
  ],
  "summary": {
    "overallAiRisk": "Low|Medium|High|null",
    "notes": "..."
  },
  "extensions": {}
}
```

## Schema rules

- The shared fields above are mandatory unless the schema explicitly allows `null`.
- Use `extensions` for category-specific structures instead of reshaping the core schema.
- Do not add ad-hoc top-level properties outside the shared contract.
- Use `coverage.status = not-present` when the category truly has no implementation in the project.
- Use `coverage.status = unable-to-assess` when the code exists but the audit could not be completed reliably.
- `docReferences` may contain URLs or stable doc identifiers, but be consistent within a single file.
- `inventory`, `consistencyChecks`, `findings`, and `recommendations` may be empty arrays when justified.

## Deterministic output requirements

The analysis skill must produce deterministic artifacts for identical inputs.

- Category key values must use the canonical order: `admin-ui`, `page-builder`, `email-builder`, `form-builder`, `global-extensibility`.
- Arrays of objects must use stable sort keys documented in JSON Schema descriptions (for example, title/name, then identifier/path).
- `generatedAtUtc` is expected to change per run; non-time fields should remain predictably ordered.
- Do not generate random IDs for analysis artifacts. If IDs are needed, derive them from stable inputs.
- Preserve explicit unknowns as `null` where allowed instead of inventing values.

These deterministic rules are part of the schema contract and should be duplicated in any prompt instructions that emit artifacts.

## Validation responsibility split

Use a split model to avoid overloading the analysis prompt while still enforcing quality:

- Analysis skill: perform lightweight self-validation before writing files (required fields, enum values, canonical category names, deterministic ordering checks).
- Report skill: perform strict prerequisite and schema validation. If required artifacts are missing or invalid, stop immediately and report actionable errors.

The report skill must not perform discovery or fresh analysis. It only validates artifacts and prepares report assets.

## Representing naming consistency

File, folder, and class naming consistency is required analysis data, but it should be represented using the existing shared schema rather than a new top-level field:

- Store concrete naming evidence in `inventory[].files`, `inventory[].relatedFiles`, and `consistencyChecks[].evidence`.
- Represent each naming rule as one or more `consistencyChecks` entries (for example, folder layout convention, class suffix convention, identifier-prefix convention).
- If naming inconsistencies create actionable risk, include them in `findings` and corresponding `recommendations`.
- Use `extensions` only when a category needs additional naming detail that cannot be expressed by `inventory` + `consistencyChecks` + `findings`.

## Analysis index schema

`analysis/analysis-index.json` should use this shape:

```json
{
  "projectPath": "...",
  "outputRoot": "...",
  "generatedAtUtc": "...",
  "selectedCategories": ["..."],
  "availableCategoryArtifacts": [
    {
      "category": "...",
      "path": "admin-ui.json",
      "coverageStatus": "analyzed|not-present|partially-covered|unable-to-assess"
    }
  ],
  "notYetAnalyzedCategories": ["..."],
  "docsReferencesByCategory": {
    "admin-ui": ["..."],
    "page-builder": ["..."]
  }
}
```
