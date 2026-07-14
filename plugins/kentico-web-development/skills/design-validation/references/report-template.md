# Report template

One JSON file per page:

- `comparison` — `name`, `design`, `live` (incl. `httpStatus`), `viewport`, `language` (`requested`, `detected`, **`fallbackSuspected`** — when true, fix the translation before trusting any other finding), `ignoreSelectors`.
- `summary` — `totals`, `bySeverity`, `headline`.
- `findings[]` — sorted by severity; key fields:

| Field | Meaning |
| --- | --- |
| `classificationHint` | Suggested root-cause class: `content` \| `serving` \| `styling` \| `unknown` (no rule matched — inspect both sides manually) — a heuristic; [classification.md](classification.md) makes the final call. |
| `design` / `live` | Locators per side: `selector`, `snippet`, `value`. Locate by **snippet first** — live selectors reflect developer-defined widget wrappers. |
| `expected` / `actual` | Design-side vs live-side value. |
| `details.kind` | Precise finding kind (below). |
| `details.changedProperties` | `style-delta` only: each differing CSS property with both values. |

## Finding kinds

| `details.kind` | Category | Typical root cause |
| --- | --- | --- |
| `text-mismatch` | content | Wrong field value, translation, or hard-coded view text. |
| `missing-leaf` | content | Empty field or unlinked item in a rendered block. |
| `extra-leaf` | content | Leftover/placeholder content, or extra widget markup. |
| `language-fallback` | content | Missing language variant — served silently with HTTP 200. |
| `unresolved-url` | content | Literal `~/` reached the browser — the view doesn't resolve URLs (serving fix). |
| `missing-block` / `extra-block` | structure | Widget not/wrongly placed, wrong section or template, empty view component. |
| `missing-landmark` / `extra-landmark` | structure | Layout view (`_Layout.cshtml`) or page template differs. |
| `heading-hierarchy` | structure | View renders the wrong heading tag. |
| `style-delta` | style | Stylesheet rule differs — see `changedProperties`. |

## Gotchas

- Language fallback returns HTTP 200 — only `fallbackSuspected` reveals it.
- Widget markup is developer-defined — trust snippets over selectors.
- Rich text (`div.fr-view`) is compared as one content unit — differences inside it are editor content edits, not view fixes.
