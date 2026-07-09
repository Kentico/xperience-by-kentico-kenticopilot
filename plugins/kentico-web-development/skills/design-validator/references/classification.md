# Classification

Every finding is classified into one of three layers, which can each go wrong independently. The report's `classificationHint` is a heuristic; verifying against the checks below makes the final call.

1. **Content** — content items and their fields in the Content hub / content tree (per language variant).
2. **Serving** — content → HTML: routing → page template → Page Builder (editable areas → sections → zones → widgets) → Razor views / view components.
3. **Styling** — the site's stylesheets.

## CONTENT

**Findings:** `text-mismatch`, `missing-leaf`, `extra-leaf`, `language-fallback`.

**Common issues:**

- Item missing or not **published** (drafts don't render live).
- Field doesn't have the expected value.
- **Language variant** missing or untranslated.
- Linked/reusable item: reference missing, or the linked item unpublished in that language.

**Not content:**

- The value in Xperience is already correct but the page shows something else — the view renders the wrong field, truncates, or hard-codes text → serving.
- UI chrome (labels, buttons) is often a missing `.resx` resource string, not a content item → serving.

## SERVING

**Findings:** `missing-block`, `extra-block`, `missing-landmark`, `extra-landmark`, `heading-hierarchy`, `unresolved-url`.

**Common issues:**

- Page Builder widget not placed in the right editable area/section/zone, or the page uses the wrong **page template**.
- A view / view component under `Components/`/`Views/` doesn't render the block, or `_Layout.cshtml` / the page template view lacks a whole landmark.
- Extra block: an editor-placed widget not in the design, debug/placeholder markup, or an outdated design.
- `unresolved-url`: the view outputs `~/` unresolved into the HTML.
- Wrong heading level in the widget/component view.

**Not serving:**

- The view is fine but its query returns empty data → content.

## STYLING

**Findings:** `style-delta`.

**Common issues:**

- A CSS/SCSS rule differs from the design.

**Not styling:**

- The delta comes from a missing class on the element (compare class lists via the selectors) → serving.
- Many small deltas page-wide from a stylesheet/bundle or font failing to load → serving.
