# Content retrieval — performance & scaling

Per-request cost is driven by how much content-graph the render resolves, not by page or site size. Control it with the levers below, roughly in order of impact.

## 1. Linked-item depth is the most expensive dial

Linked-item depth — `LinkedItemsMaxLevel` on the retriever's `Retrieve*Parameters`, or `WithLinkedItems(maxLevel)` on the content item query builder (`ContentItemQueryBuilder`) — resolves linked items recursively, so cost grows **combinatorially, not linearly** with each level. Evaluate whether you actually need the depth you set, and keep it to the minimum the render requires.

## 2. Caching

When using `IContentRetriever`, caching is automatic. However, with `ContentItemQueryBuilder` / `IContentQueryExecutor`, you need to handle caching yourself using the `IProgressiveCache`.

## 3. Custom queries need a unique cache-key suffix

Custom `additionalQueryConfiguration` (`Where` / `TopN` / `OrderBy` / `Columns`) is **not** folded into the auto cache key. Pass a `cacheItemNameSuffix` (on `RetrievalCacheSettings`) built from the parameter values, or distinct queries collide on one cache entry and serve the wrong content.

## 4. Retrieve only the columns you need

Use `.Columns(...)` (and `UrlPathColumns()` for URL-only data) instead of pulling every field. Projection reaches the top-level type only, so it won't trim linked-item payloads — for those, see Known limitations.

## 5. Page large result sets

`TopN(n)` for fixed small sets; `Offset(offset, fetch)` for paging (**requires `OrderBy`**, **can't combine with `TopN`**). Don't fetch whole collections and trim in memory.

## Known limitations

These are edge cases at scale — linked items with caching remain the norm. Consider an alternative approach only for a query that measurably suffers.

- Column projection (`Columns()`) doesn't reach linked items — depth loads every field of every linked type. Use `LinkedFrom` / `LinkedFromSchemaField` only to prune unwanted branches: cut the linked items you don't need at a given level and load the rest in a separate retrieval.
- Multi-type / schema retrieval (`RetrieveContentOfContentTypes`, `RetrieveContentOfReusableSchemas`, all-pages methods) unions every included type's own fields, so many types slow down. Set `IncludeContentTypeFields = false` to load only the common fields (metadata + reusable schema) and skip the per-type union.

## Signatures

Confirm exact names and overloads in the [IContentRetriever API reference](https://api-reference.kentico.com/api/Kentico.Content.Web.Mvc.IContentRetriever.html); caching details in the [data caching](https://docs.kentico.com/documentation/developers-and-admins/development/caching/data-caching) and [cache dependencies](https://docs.kentico.com/documentation/developers-and-admins/development/caching/cache-dependencies) docs.
