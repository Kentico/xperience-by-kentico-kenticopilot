---
name: analyze-components
description: "Analyzes Xperience by Kentico component consistency across a project (admin UI, builders, extenders, and related platform customizations) and produces JSON + HTML reports with findings, risks, and recommendations."
argument-hint: "Path to the Xperience by Kentico project folder to analyze"
compatibility: "Requires Kentico Docs MCP"
---

You are tasked with auditing component consistency in an Xperience by Kentico project to improve maintainability and AI-assisted development outcomes.

The goal is to identify inconsistent implementation patterns that reduce predictability for developers and AI agents. Inconsistent patterns make generated code less reliable and increase refactoring overhead.

This skill must remain project-agnostic. Do not assume sample-project-specific file paths, naming conventions, or architecture unless discovered in the current workspace.

## Mandatory first step

Use Kentico Docs MCP to research current guidance for all relevant component categories before auditing the codebase.

- Prioritize official docs for:
	- Page Builder widgets, sections, templates, and personalization condition types
	- Email Builder templates, sections, widgets, editable areas, and markup guidance
	- Form Builder components, sections, validation rules, and visibility conditions
	- Admin UI customization (applications, pages, page templates, commands, extenders, UI form components, client React modules)
	- Global customization patterns (global event handlers, scheduled tasks, custom modules, provider customizations)

If Kentico docs guidance and existing code differ, report both and call out the drift explicitly.

## Scope

Analyze and summarize all discovered Xperience components, including at minimum:

- Admin UI applications
- Admin UI pages and page templates
- Admin UI page commands and page extenders
- Admin UI form components (editing components), validation rules, visibility conditions
- Admin UI custom client React components/modules
- Page Builder widgets
- Page Builder sections
- Page Builder templates
- Page Builder personalization condition types
- Email Builder templates and editable areas
- Email Builder sections
- Email Builder widgets
- Form Builder components
- Form Builder sections
- Form Builder validation rules
- Form Builder visibility conditions
- Global event handlers
- Scheduled tasks
- Custom module/object-type extensions and provider customizations

Include any other Xperience platform-specific component categories discovered in the codebase.

## Analysis process

### 1. Discover implementations

Find all implementations by category and build an inventory with:

- Component category
- Component identifier/name
- Registration mechanism and metadata
- Source files
- Related files (properties models, views/razor files, client modules, styles/scripts, tests)

### 2. Run consistency checks by category

Apply the following consistency checks to each component category where applicable.

- Naming and identifiers:
	- identifier uniqueness and prefixing strategy
	- file/class/component naming consistency
	- namespace/module naming consistency
- Registration and discoverability:
	- required registration attributes present
	- registration metadata completeness (name, description, icon, etc.)
	- registration location consistency (global scope where required)
- Properties and editing experience:
	- typed property model presence and consistency
	- editing components and ordering conventions
	- default values and null-safety
	- validation rule coverage for required inputs
- Data and dependency patterns:
	- service usage and data access consistency
	- caching and dependency strategy consistency where relevant
	- avoidance of duplicated querying logic
- Rendering and composition patterns:
	- view/component structure consistency
	- correct use of zones/areas and supported markup rules
	- separation of concerns between rendering and business logic
- Reliability and safety:
	- error handling and guard checks
	- asynchronous usage consistency
	- use of platform-supported extension points over hacks
- Documentation and testability:
	- doc comments and intent clarity
	- unit/integration/UI test presence for critical customizations
	- determinism and repeatability for scheduled/background logic

### 3. Evaluate AI code generation risk

For each inconsistency, assess AI generation risk:

- Low: Minor variance; AI can still infer patterns reliably
- Medium: Mixed patterns likely to cause partial mismatch
- High: Contradictory patterns likely to produce incorrect or inconsistent generated code

### 4. Capture missing component types and coverage gaps

Report missing categories from the codebase audit scope (for example, category not present, or present but not registered/used consistently).

### 5. Produce recommendations

For each medium/high issue, include:

- Recommendation summary
- Why it matters for consistency and AI-assisted development
- Suggested target pattern
- Migration strategy
- Estimated remediation risk (Low/Medium/High)

## Best-practice checklist by component type

Build best-practice checks using docs-backed guidance. Include at least the following checks in your audit.

### Admin UI components

- Ensure server-side definitions and client-side components are coherently paired.
- Prefer dedicated admin customization assemblies and organized module boundaries.
- Keep extenders focused; avoid large cross-cutting logic in extender hooks.
- Validate command handling and result flow consistency.
- Use consistent React/TypeScript typing and exported module contracts.

### Page Builder widgets/sections/templates/personalization

- Register all custom components with stable, unique identifiers.
- Keep components in global scope (not in Areas) where required by platform guidance.
- Use typed properties and editing components with meaningful labels and defaults.
- Ensure sections include valid widget zones and templates include required builder assets.
- Keep rendering simple; move non-trivial logic into view components/services.
- Implement personalization condition types with clear, testable evaluate logic.

### Email Builder templates/sections/widgets

- Keep markup strategy consistent across project (MJML-only or HTML-only; do not mix).
- Use correctly structured editable areas, sections, and widget zones.
- Register components with clear metadata and stable identifiers.
- Use typed property models with validation and sensible defaults.
- Ensure section/layout rules are respected (for example MJML column placement rules).

### Form Builder components/sections/rules

- Register components and sections with consistent metadata and unique identifiers.
- Use typed properties and robust validation rules for field configuration.
- Keep view and logic responsibilities separated; avoid overloading partial views.
- Ensure expected scripts/styles are included and scoped correctly.
- Validate editor and live-site behavior parity for key components.

### Global extensibility components

- Register modules/events/tasks through supported extension points.
- Keep global event handlers minimal, deterministic, and side-effect aware.
- Use scheduled tasks for recurring jobs with idempotent and observable behavior.
- Prefer provider customization patterns that align with analyzer/best-practice guidance.
- Avoid hidden coupling between customization points.

## Output

Output both artifacts:

1. JSON document
2. HTML report

### JSON requirements

Generate a machine-readable JSON document with this top-level shape:

```json
{
	"analysisMetadata": {
		"projectPath": "...",
		"generatedAtUtc": "...",
		"docsReferences": ["..."]
	},
	"summary": {
		"overallConsistencyScore": 0,
		"overallAiRisk": "Low|Medium|High",
		"componentCategoryCounts": {}
	},
	"categories": [
		{
			"category": "...",
			"inventory": [
				{
					"name": "...",
					"identifier": "...",
					"files": ["..."],
					"registration": "..."
				}
			],
			"checks": [
				{
					"check": "...",
					"status": "pass|warning|fail",
					"details": "...",
					"evidence": ["..."]
				}
			],
			"bestPractices": [
				{
					"practice": "...",
					"status": "pass|warning|fail",
					"notes": "..."
				}
			],
			"inconsistencies": [
				{
					"title": "...",
					"severity": "Low|Medium|High",
					"aiRisk": "Low|Medium|High",
					"impact": "...",
					"evidence": ["..."]
				}
			],
			"recommendations": [
				{
					"summary": "...",
					"targetPattern": "...",
					"migrationPath": ["..."],
					"remediationRisk": "Low|Medium|High"
				}
			]
		}
	],
	"missingOrUncoveredCategories": ["..."],
	"prioritizedActions": [
		{
			"priority": "Critical|Important|Nice-to-have",
			"action": "...",
			"aiRiskReduction": "...",
			"remediationRisk": "Low|Medium|High"
		}
	]
}
```

### HTML requirements

Generate a well-structured HTML document designed for readability and decision making.

- Include:
	- Executive summary cards (score, risk, category counts)
	- Category-by-category findings table
	- Inconsistency severity and AI-risk visualization
	- Best-practice compliance matrix
	- Prioritized action plan section
- Use clean semantic HTML and embedded CSS for a polished, readable report.
- Keep style professional and minimal, with strong visual hierarchy.
- Ensure the report is usable without external CSS/JS dependencies.

### Final response format

Return:

1. Paths to the generated JSON and HTML artifacts
2. A concise summary of key findings:
	 - top high-risk inconsistencies
	 - missing component categories
	 - highest-value standardization actions

Do not output only narrative text when this skill runs. The JSON and HTML artifacts are mandatory deliverables.
