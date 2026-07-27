# CLI guidance

`<scripts>` is this skill's `scripts/` directory.

## Setup

Needed only once, before the first run — skip when `<scripts>/node_modules` already exists.

```sh
cd <scripts>
npm ci
npx playwright install chromium
```

## Running

```sh
node <scripts>/compare.ts --design <file|folder> --live <url> [options]
```

Run with `--help` for all options. Two modes:

- **Single page** — `--design <file>` + `--live <url>`: validate one page against one design file.
- **Folder batch** — `--design <folder>` + `--live <base-url>`: validate a whole design set in one run; every `*.html` (recursive) pairs with a live URL derived from its relative path (`index.html` → base, `about.html` → `<base>/about`).

Guidance `--help` doesn't spell out:

- Build `--live` URLs with the Xperience language prefix (primary `/page`, others `/<lang>/page`) and pass `--language` to enable fallback detection.
- **Language-fallback detection depends entirely on `<html lang>`**: if the site's layout doesn't set it, no fallback is detected (`--language` is then a no-op).
- Pass a project-local `--out` — the default `<scripts>/reports/` does not survive plugin updates.
- `--ignore <selector>` excludes dynamic regions (cookie banners, personalization) on both sides.
- `font-family` values are fallback lists (`"Inter", Arial, sans-serif`); only the first family is compared, so a differing fallback tail never produces a finding.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Comparison ran (findings may exist — read the report). |
| 1 | Usage or runtime error, incl. pages that failed to load in a batch (the rest still complete). |
| 2 | `--fail-on` matched. |
