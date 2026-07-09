# CLI guidance

`<scripts>` is this skill's `scripts/` directory.

## Setup

```
cd <scripts>
npm ci
npx playwright install chromium
```

## Running

```
node <scripts>/compare.ts --design <file|folder> --live <url|folder> [options]
```

Run with `--help` for all options. Three modes:

- **Single page** — `--design <file>` + `--live <url>`.
- **Folder batch** — `--design <folder>` + `--live <base-url>`: every `*.html` (recursive) pairs with a URL from its relative path.
- **Local live folder** — `--live <folder>` is served statically and compared path-for-path.

Guidance `--help` doesn't spell out:

- Build `--live` URLs with the Xperience language prefix (primary `/page`, others `/<lang>/page`) and pass `--language` to enable fallback detection.
- Pass a project-local `--out` — the default `<scripts>/reports/` does not survive plugin updates.
- `--ignore <selector>` excludes dynamic regions (cookie banners, personalization) on both sides.
- Font families are compared by the first family only — fallback-tail differences are ignored by design.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Comparison ran (findings may exist — read the report). |
| 1 | Usage or runtime error, incl. pages that failed to load in a batch (the rest still complete). |
| 2 | `--fail-on` matched. |
