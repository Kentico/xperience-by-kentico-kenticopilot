---
name: content-retrieval
description: "Xperience by Kentico (XbK) content retrieval — reading published content (pages, reusable content items, reusable-schema items) in live-site / MVC code. Use whenever code fetches content, turns a Combined content selector / Page selector selection into data, or when a content query is slow under load."
compatibility: "Requires Kentico Docs MCP"
---

# Content retrieval

This skill is a **map, not a manual** — the decision rules below, then pointers to the docs and API reference for the details. Prefer the linked references over reconstructing API signatures from memory.

- **`references/content-retrieval-docs.md`** — the documentation map: every relevant docs page and the API reference, each with a "when to read" hint.
- **`references/performance.md`** — how to keep retrieval fast under load and the known API limitations. **Read this before setting any linked-items depth above 0, writing a custom query, or diagnosing a page that's slow under load.**

## The two APIs — prefer the retriever

Two APIs read published content. **Reach for `IContentRetriever` first; drop to the content item query API only when you outgrow it.**

- **`IContentRetriever`** (`Kentico.Content.Web.Mvc`, injected via DI) — the default for almost all retrieval. It supplies the defaults you usually want (language fallback, preview, URL paths, linked-item depth), **caches results implicitly**, and exposes every default as an override hook — so advanced filtering/ordering/tags/smart folders is *not* a reason to leave it.
- **Content item query API** (`ContentItemQueryBuilder` + `IContentQueryExecutor`, `CMS.ContentEngine`) — the same engine with nothing pre-decided and **no implicit caching**. Reach for it only for advanced query composition the retriever's hooks can't express.
- **Non-content objects** (users, settings, custom module classes) — use the ObjectQuery API; neither content API applies.

- **Combined content selector** returns `ContentItemGUID`s (even for pages) → load with `RetrieveContent*ByGuids`. **Page selector** returns `WebPageItemGUID`s → load with `RetrieveAllPagesByGuids`/`RetrievePagesByGuids`.
