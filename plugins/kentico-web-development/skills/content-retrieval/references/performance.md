# Content retrieval — performance & scaling

Per-request cost is driven by how much content-graph the render resolves, not by page or site size. Control it with the levers below, roughly in order of impact.

## 1. Linked-item depth is the most expensive dial

`LinkedItemsMaxLevel` (query-builder: `WithLinkedItems(maxLevel)`) defaults to `0` — no linked items. Every level resolves links recursively, so cost grows **combinatorially, not linearly**.

- Start at `0`; raise depth **only on the specific query** that renders that graph, never globally or "just in case."
- Treat **depth 3+ as a design smell** that needs justification.
- Set depth **per query, not on a shared repository method** — use separate "list" vs "detail" retrieval paths so a listing page doesn't pay for a graph only the detail page renders.

## 2. Lean on the retriever's implicit caching — don't defeat it

`IContentRetriever` caches results implicitly (default 10 min via `ContentRetrieverCacheOptions.DefaultCacheExpiration`, or per-call via `RetrievalCacheSettings`). A deep graph *with* caching is fine; the same graph uncached is the failure mode.

- **Prefer `IContentRetriever` for read paths** — the raw `ContentItemQueryBuilder` / `IContentQueryExecutor` has **no implicit caching** (you'd wrap it in `IProgressiveCache` yourself).
- **Don't use `RetrievalCacheSettings.CacheDisabled`** unless genuinely required. Preview already disables caching automatically — don't disable it "for preview."
- The retriever **auto-wires linked-item cache dependencies** (`ILinkedItemsDependencyAsyncRetriever` / `IWebPageLinkedItemsDependencyAsyncRetriever`) so cached deep graphs invalidate when a linked item changes; hand-rolled caching around raw queries does not, and goes stale.

## 3. Custom queries need a unique cache-key suffix

Custom `additionalQueryConfiguration` (`Where` / `TopN` / `OrderBy` / `Columns`) is **not** folded into the auto cache key. Pass a `cacheItemNameSuffix` (on `RetrievalCacheSettings`) built from the parameter names plus their values, or distinct queries collide on one cache entry and serve the wrong content.

## 4. Retrieve only the columns you need

Use `.Columns(...)` (and `UrlPathColumns()` when you only need URL data) instead of pulling every field — most impactful on linked-item queries, where "all columns × all linked types × depth" balloons the payload. Note column projection applies to the top-level type only (see limitation A).

## 5. Page large result sets

`TopN(n)` for fixed small sets; `Offset(offset, fetch)` for paging (**requires an `OrderBy`**, **can't be combined with `TopN`**). Don't fetch whole collections and trim in memory inside per-request components.

## Known limitations

**A. Columns can't be limited for linked items.** `Columns()` projects the top-level type only — no API projects columns on linked items or picks which reference field to load links from. So `WithLinkedItems` loads all fields of all linked types up to the depth. *Workaround:* depth `0` + load links with a second retrieval using `LinkedFrom` / `LinkedFromSchemaField` (both on the `additionalQueryConfiguration` hook, so you keep implicit caching), projecting only the columns you need.

**B. Schema / many-content-type queries fan out into `UNION`s.** `RetrieveContentOfContentTypes`, `RetrieveContentOfReusableSchemas`, and the all-pages methods union every included type's field set. Many types sharing one schema plus a non-trivial depth degrades badly. *Workaround:* keep depth and columns tight, and limit content-type-field inclusion via the retriever's parametrization.

## Signatures

Confirm exact names and overloads in the [IContentRetriever API reference](https://api-reference.kentico.com/api/Kentico.Content.Web.Mvc.IContentRetriever.html); caching details in the [data caching](https://docs.kentico.com/documentation/developers-and-admins/development/caching/data-caching) and [cache dependencies](https://docs.kentico.com/documentation/developers-and-admins/development/caching/cache-dependencies) docs.
