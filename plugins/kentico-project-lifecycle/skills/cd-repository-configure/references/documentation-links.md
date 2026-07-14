# Documentation links

Fetch these on demand via the **Kentico Docs MCP**. Each link has a **When to read** hint — fetch only the pages the current task needs.

- **Exclude objects from CI/CD**: <https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories>
  - When to read: a filter element's semantics are unclear — `IncludedObjectTypes` vs. `ObjectFilters` vs. `IncludedContentItemsOfType` / `ContentItemFilters`, wildcard patterns, or how the filters combine.
- **Reference – CI/CD object types**: <https://docs.kentico.com/documentation/developers-and-admins/ci-cd/reference-ci-cd-object-types>
  - When to read: a changed CI path has no entry in `ci-path-mapping.md`. This is the authoritative object type list, including which types support code name filtering.
- **Repository configuration templates**: <https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/repository-configuration-templates>
  - When to read: the user wants a ready-made config for a common CD scenario instead of a change-scoped one.
- **Migrate CI/CD repository.config to v2**: <https://docs.kentico.com/documentation/developers-and-admins/ci-cd/configure-ci-cd-repositories/config-v2-migration>
  - When to read: workflow step 1 detected a v1 config and the user wants help applying the migration.
- **Store objects to a CD Repository**: <https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-deployment#store-objects-to-a-cd-repository>
  - When to read: `Export-DeploymentPackage.ps1` is not available and the CD store must be run directly.
- **Content sync**: <https://docs.kentico.com/documentation/business-users/content-sync>
  - When to read: the deployment scope contains only content item data and you are recommending Content sync as the simpler alternative.
