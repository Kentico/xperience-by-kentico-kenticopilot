# Content retrieval — documentation map

Look up the page for the task instead of reconstructing API details from memory — signatures and defaults drift between releases. Each link has a **When to read** hint. If the **Kentico Docs MCP** (`kentico_docs_search` / `kentico_docs_fetch`) is available, use it to fetch the current page content; the URLs below are the canonical anchors.

## Choosing the API

- **Content retrieval (section overview)**: https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval
  - When to read: to confirm you're reading *published content in live-site/MVC code* and to see the two APIs side by side.
- **ContentRetriever API (guide)**: https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-retriever-api
  - When to read: the default path for almost all retrieval — how the retriever supplies defaults and exposes override hooks. Read before writing retrieval code.
- **Content item query API (guide)**: https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-item-query-api
  - When to read: only for the two cases the retriever can't cover — eliminating the forced per-type-field `UNION` on large multi-type/schema queries, or owning the whole row→model binding loop (`GetResult` + a custom `IContentQueryModelTypeMapper`). No implicit caching, so you wrap it yourself.
- **ObjectQuery API**: https://docs.kentico.com/documentation/developers-and-admins/api/objectquery-api
  - When to read: the data is *not* content — users, settings, custom module classes.

## Retrieving pages

- **Retrieve page content**: https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval/retrieve-page-content
  - When to read: pulling pages from a channel's content tree; also the source for `PathMatch` (`Single`/`Children`/`Section`/`SkipChildren`/`SkipSection`) filtering.
- **Retrieve page URLs**: https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval/retrieve-page-content/retrieve-page-urls
  - When to read: you need a page's live URL. Explains `IncludeUrlPath` / URL-only column loading.

## Retrieving reusable content items

- **Retrieve content items**: https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval/retrieve-content-items
  - When to read: fetching reusable items from the content hub (one type, several types, or by reusable schema); covers workspaces and by-GUID fetches.
- **Reusable field schemas**: https://docs.kentico.com/documentation/developers-and-admins/development/content-types/reusable-field-schemas
  - When to read: retrieving across many content types that share a schema — see `references/performance.md` first, as schema queries fan out into large `UNION`s.

## Selectors → turning a selection into data

- **Admin UI form components (selectors reference)**: https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-form-components/reference-admin-ui-form-components
  - When to read: a model property uses a selector attribute and you must load the selection. Confirms each selector's C# type and GUID — the Combined content selector (`ContentItemGUID`) and Page selector (`WebPageItemGUID`) are **not interchangeable**; crossing them returns an empty result with no exception.

## Language, preview, security

- **Languages & fallbacks**: https://docs.kentico.com/documentation/developers-and-admins/configuration/languages#language-fallbacks
  - When to read: unexpected language/fallback content, or you need to force a language. Fallbacks are **not** applied to smart-folder retrieval.
- **Secure content items**: https://docs.kentico.com/documentation/business-users/content-hub/content-items#secure-content-items
  - When to read: secured items missing from results. `IncludeSecuredItems` defaults to `false`; then gate display with `item.HasAccess(User)`.
- **Workspaces**: https://docs.kentico.com/documentation/developers-and-admins/configuration/users/role-management/workspaces
  - When to read: scoping reusable-item retrieval to specific workspaces (`WorkspaceNames`).

## Filtering, tags, smart folders

- **Taxonomies**: https://docs.kentico.com/documentation/developers-and-admins/configuration/taxonomies
  - When to read: filtering by tags (`WhereContainsTags`); a Tag selector feeds `IEnumerable<Guid>`.
- **Smart folders**: https://docs.kentico.com/documentation/business-users/content-hub/content-hub-folders#smart-folders
  - When to read: retrieving a curated smart-folder set. Scope with `InSmartFolder` on `RetrieveContent` / `RetrieveContentOfContentTypes` (a *content* method, never a page method); one `InSmartFolder` per query.
- **Filter content by taxonomies (training guide)**: https://docs.kentico.com/guides/development/advanced-content/filter-content-based-on-taxonomies
  - When to read: the two-step "reusable items by schema+tags, then the pages that link them" pattern.

## Caching & performance

- **Data caching**: https://docs.kentico.com/documentation/developers-and-admins/development/caching/data-caching
  - When to read: hand-rolling caching around raw query-API code (the retriever caches implicitly; the raw builder does not).
- **Cache dependencies**: https://docs.kentico.com/documentation/developers-and-admins/development/caching/cache-dependencies
  - When to read: making cached deep-graph content invalidate when a linked item changes.
- **`references/performance.md`** (in this skill)
  - When to read: before raising linked-items depth, writing a custom query, or diagnosing a slow page.

## API signature reference

- **IContentRetriever (methods, overloads, extension methods)**: https://api-reference.kentico.com/api/Kentico.Content.Web.Mvc.IContentRetriever.html
  - When to read: to confirm an exact method name, its generic type parameters, or an overload. The `*ByGuids` methods (`RetrieveContentByGuids`, `RetrieveContentOfContentTypesByGuids`, `RetrieveContentOfReusableSchemasByGuids`, `RetrievePagesByGuids`, `RetrieveAllPagesByGuids`) are **extension methods** — look under the page's *Extension Methods* section, not the interface's own members.
- **Reference — ContentRetriever API (parameter tables)**: https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/reference-content-retriever-api
  - When to read: the full property list and defaults of each `Retrieve*Parameters` object.
- **Reference — Content item query (fluent methods)**: https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/reference-content-item-query
  - When to read: the query builder's fluent surface (`ForContentType`, `ForContentTypes`, `Where`, `Columns`, `TopN`, `Offset`, `InSmartFolder`, …).
- **Generate code files (models + `RegisterContentTypeMapping`)**: https://docs.kentico.com/documentation/developers-and-admins/api/generate-code-files-for-system-objects
  - When to read: to produce or understand the generated content-type model classes the retriever binds to.
- **Runnable API examples**: https://docs.kentico.com/api/
  - When to read: you want a complete, copy-runnable example rather than a signature.
