---
name: "design-validation"
description: "Validates a live Xperience by Kentico site against static HTML designs — a bundled Playwright script compares each design page with the rendered live page (content, structure, computed styles), and each difference is classified as a content, serving, or styling issue with the root cause and fix location identified. Use when asked to validate, QA, or compare pages against a design/mockup/static HTML, or why a page differs from the design; also proactively after changing a page, template, widget, view component, or stylesheet when a design for it exists."
argument-hint: "[design-file-or-folder] [live-site-url]"
compatibility: "Requires Node.js 22.18+ and npm 11.10+; first run downloads Playwright Chromium. Kentico Management MCP recommended."
---

# Design validation

The bundled script compares design vs live deterministically and writes a JSON report per page; classify each finding as **content**, **serving**, or **styling** and drive the fix. `<skill-dir>` is the directory containing this SKILL.md.

## Workflow

1. Locate the design HTML (file or folder) and the live site URL from the running site or `launchSettings.json`.
2. Read `references/cli-guidance.md` for setup and run, and `references/report-template.md` for the report format.
3. Set up all necessary dependencies for the scripts per the Setup section of `references/cli-guidance.md` (if not done before).
4. Run the scripts to compare design vs live.
5. Classify each finding with the playbooks in `references/classification.md`.
6. Report findings grouped by classification, then severity, with root cause and fix location.
