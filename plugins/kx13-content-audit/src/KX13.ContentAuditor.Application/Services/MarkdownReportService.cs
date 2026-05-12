using System.Text;

using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Application.Services
{
    public class MarkdownReportService
    {
        public string GenerateReport(KX13ProjectContent model)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Content Model Report");
            sb.AppendLine();

            if (model.Failures.Count > 0)
            {
                RenderFailuresSection(sb, model.Failures);
            }

            RenderSitesSection(sb, model.Sites);
            RenderPageTreeSections(sb, model.Sites);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderPageTypesSection(sb, model);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderPageBuilderComponentsSection(sb, model.PageBuilderComponentCatalogue);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderContentReferenceChains(sb, model.ContentReferenceGraph);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderRelationshipsSection(sb, model.RelationshipNames, model.Relationships);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderCustomModulesSection(sb, model.AllCustomModules);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderCustomTablesSection(sb, model.AllCustomTables);

            sb.AppendLine("---");
            sb.AppendLine();

            RenderFormsSection(sb, model.Sites);

            return sb.ToString();
        }

        private static void RenderFailuresSection(StringBuilder sb, List<AuditFailure> failures)
        {
            sb.AppendLine("## Auditor Warnings");
            sb.AppendLine();


            sb.AppendLine($"- **Encountered failures:** {failures.Count}");
            sb.AppendLine();
            sb.AppendLine("| Category | Entity Type | Identifier | Context | Error |");
            sb.AppendLine("| -------- | ----------- | ---------- | ------- | ----- |");

            foreach (var failure in failures
                .OrderBy(f => f.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.EntityType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.EntityIdentifier, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine(
                    $"| {Esc(failure.Category)} | {Esc(failure.EntityType)} | {Esc(failure.EntityIdentifier)} | {Esc(failure.Context)} | {Esc(failure.ErrorMessage)} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // ── Sites ──────────────────────────────────────────

        private static void RenderSitesSection(StringBuilder sb, List<Site> sites)
        {
            sb.AppendLine("## Sites");
            sb.AppendLine();
            sb.AppendLine("| Site | Code Name | Domain | Default Culture | All Cultures |");
            sb.AppendLine("| ---- | --------- | ------ | --------------- | ------------ |");

            foreach (var site in sites)
            {
                string cultures = site.SiteCultures.Count > 0
                    ? string.Join(", ", site.SiteCultures)
                    : "—";
                sb.AppendLine($"| {Esc(site.SiteDisplayName)} | `{Esc(site.SiteName)}` | {Esc(site.SiteDomainName)} | {Esc(site.SiteDefaultCultureCode)} | {cultures} |");
            }

            sb.AppendLine();
        }

        // ── Page Trees ─────────────────────────────────────

        private static void RenderPageTreeSections(StringBuilder sb, List<Site> sites)
        {
            foreach (var site in sites)
            {
                var flatNodes = FlattenTree(site.ContentTree);
                var linkedNodes = flatNodes
                    .Where(node => node.NodeLinkedNodeId.HasValue)
                    .OrderBy(node => node.NodeAliasPath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                sb.AppendLine($"### {Esc(site.SiteDisplayName ?? site.SiteName)}");
                sb.AppendLine();
                sb.AppendLine("```text");
                RenderAsciiTree(sb, site.ContentTree, "", isRoot: true);
                sb.AppendLine("```");
                sb.AppendLine();

                if (site.SiteCultures.Count > 0)
                {
                    sb.AppendLine($"> **Cultures:** {string.Join(", ", site.SiteCultures)} — tree structure is identical across cultures.");
                    sb.AppendLine();
                }

                int pageCount = CountNodes(site.ContentTree);
                var typesInUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var node in flatNodes)
                {
                    if (node.PageTypeClassName is not null) typesInUse.Add(node.PageTypeClassName);
                }

                sb.AppendLine($"- **Total pages:** {pageCount}");
                sb.AppendLine($"- **Page types in use:** {typesInUse.Count}");
                sb.AppendLine($"- **Linked pages:** {linkedNodes.Count}");
                sb.AppendLine($"- **Cultures:** {(site.SiteCultures.Count > 0 ? string.Join(", ", site.SiteCultures) : "—")}");
                sb.AppendLine();

                if (linkedNodes.Count > 0)
                {
                    sb.AppendLine("#### Linked Pages");
                    sb.AppendLine();
                    sb.AppendLine("| Linked Page | Linked Class | Original Page | Original Class |");
                    sb.AppendLine("| ----------- | ------------ | ------------- | -------------- |");

                    foreach (var node in linkedNodes)
                    {
                        sb.AppendLine($"| {Esc(node.NodeAliasPath)} | {Esc(node.PageTypeClassName)} | {Esc(GetLinkedOriginalPath(node))} | {Esc(GetLinkedOriginalClass(node))} |");
                    }

                    sb.AppendLine();
                }
            }
        }

        private static void RenderAsciiTree(StringBuilder sb, List<ContentTreeNode> nodes, string prefix, bool isRoot = false)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                bool isLast = i == nodes.Count - 1;

                if (isRoot && nodes.Count == 1 && node.NodeAliasPath == "/")
                {
                    // Root node
                    sb.AppendLine($"/  [{node.PageTypeClassName}]");
                    RenderAsciiTree(sb, node.Children, "", isRoot: false);
                    return;
                }

                string connector = isLast ? "└── " : "├── ";
                string childPrefix = prefix + (isLast ? "    " : "│   ");

                sb.AppendLine($"{prefix}{connector}{node.NodeAliasPath}  [{node.PageTypeClassName}]");

                RenderNodeAnnotations(sb, node, childPrefix);

                if (node.Children.Count > 0)
                {
                    RenderAsciiTree(sb, node.Children, childPrefix);
                }
            }
        }

        private static void RenderNodeAnnotations(StringBuilder sb, ContentTreeNode node, string childPrefix)
        {
            var annotations = new List<string>();
            var templateId = node.PageBuilderConfig?.Template?.Identifier;
            var widgetIds = GetWidgetIdentifiers(node);

            if (templateId is not null)
            {
                annotations.Add($"Template: {templateId}");
            }

            if (widgetIds.Count > 0)
            {
                annotations.Add($"Widgets: {string.Join(", ", widgetIds)}");
            }

            if (node.NodeLinkedNodeId.HasValue)
            {
                annotations.Add($"Linked -> {GetLinkedOriginalDescriptor(node)}");
            }

            for (int i = 0; i < annotations.Count; i++)
            {
                string connector = i == annotations.Count - 1 ? "└── " : "├── ";
                sb.AppendLine($"{childPrefix}{connector}{annotations[i]}");
            }
        }

        private static List<string> GetWidgetIdentifiers(ContentTreeNode node)
        {
            var ids = new List<string>();
            if (node.PageBuilderConfig?.EditableAreas is null) return ids;

            foreach (var area in node.PageBuilderConfig.EditableAreas)
            {
                foreach (var section in area.Sections)
                {
                    foreach (var zone in section.Zones)
                    {
                        foreach (var widget in zone.Widgets)
                        {
                            if (widget.TypeIdentifier is not null && !ids.Contains(widget.TypeIdentifier))
                                ids.Add(widget.TypeIdentifier);
                        }
                    }
                }
            }

            return ids;
        }

        private static string GetLinkedOriginalDescriptor(ContentTreeNode node)
        {
            if (node.LinkedOriginalNodeAliasPath is not null || node.LinkedOriginalClassName is not null)
            {
                string className = node.LinkedOriginalClassName ?? "Unknown";
                return $"{node.LinkedOriginalNodeAliasPath ?? "Unknown"} [{className}]";
            }

            return $"Unresolved original ({GetLinkedOriginalIdentifier(node)})";
        }

        private static string GetLinkedOriginalPath(ContentTreeNode node) =>
            node.LinkedOriginalNodeAliasPath ?? $"Unresolved ({GetLinkedOriginalIdentifier(node)})";

        private static string GetLinkedOriginalClass(ContentTreeNode node) =>
            node.LinkedOriginalClassName ?? "—";

        private static string GetLinkedOriginalIdentifier(ContentTreeNode node) =>
            node.NodeLinkedNodeSiteId.HasValue
                ? $"NodeID {node.NodeLinkedNodeId}, SiteID {node.NodeLinkedNodeSiteId}"
                : $"NodeID {node.NodeLinkedNodeId}";

        // ── Page Types ─────────────────────────────────────

        private static void RenderPageTypesSection(StringBuilder sb, KX13ProjectContent model)
        {
            sb.AppendLine("## Page Types");
            sb.AppendLine();

            var (usedTypes, unusedTypes, siteMap) = ClassifyPageTypes(model);
            var usageIndex = BuildUsageIndex(model.Sites);

            foreach (var pt in usedTypes.OrderBy(p => p.ClassName))
            {
                sb.AppendLine($"### {Esc(pt.ClassName)} — _{Esc(pt.ClassDisplayName)}_");
                sb.AppendLine();

                // Metadata block
                sb.AppendLine($"- **Class ID:** {pt.ClassId}");
                string coupledTable = !string.IsNullOrEmpty(pt.ClassTableName)
                    ? $"`{pt.ClassTableName}`"
                    : "— _(no custom data table, all data lives in CMS_Tree/CMS_Document)_";
                sb.AppendLine($"- **Coupled table:** {coupledTable}");

                string urlPattern = pt.UrlEnabled
                    ? (pt.UrlPattern ?? "—")
                    : "None — container/folder type";
                sb.AppendLine($"- **URL pattern:** {urlPattern}");
                sb.AppendLine($"- **Page Builder:** {(pt.PageBuilderEnabled ? "Enabled" : "Disabled")}");
                sb.AppendLine($"- **Inherits from:** {(string.IsNullOrEmpty(pt.InheritsFromClassName) ? "—" : $"`{pt.InheritsFromClassName}`")}");

                // Used at
                if (usageIndex.TryGetValue(pt.ClassName!, out var usages))
                {
                    sb.AppendLine($"- **Used at:** {FormatUsageSummary(usages)}");
                }

                sb.AppendLine();

                // Field table
                RenderFieldTable(sb, pt.Fields);

                // Inheritance note
                if (!string.IsNullOrEmpty(pt.InheritsFromClassName))
                {
                    sb.AppendLine();
                    sb.AppendLine($"> Note: Inherits fields from `{pt.InheritsFromClassName}`.");
                }

                sb.AppendLine();
            }

            // Unused page types table
            sb.AppendLine("### Unused Page Types");
            sb.AppendLine();

            if (unusedTypes.Count == 0)
            {
                sb.AppendLine("_None_");
            }
            else
            {
                sb.AppendLine("| Class Name | Display Name | Has Fields | Page Builder | Notes |");
                sb.AppendLine("| ---------- | ------------ | ---------- | ------------ | ----- |");

                foreach (var pt in unusedTypes.OrderBy(p => p.ClassName))
                {
                    string hasFields = pt.Fields.Count > 0 ? "Yes" : "No";
                    string pb = pt.PageBuilderEnabled ? "Yes" : "No";
                    string notes = pt.ClassName?.StartsWith("CMS.", StringComparison.OrdinalIgnoreCase) == true
                        || pt.ClassName?.StartsWith("Ecommerce.", StringComparison.OrdinalIgnoreCase) == true
                        ? "system type" : "—";
                    sb.AppendLine($"| {Esc(pt.ClassName)} | {Esc(pt.ClassDisplayName)} | {hasFields} | {pb} | {notes} |");
                }
            }

            sb.AppendLine();
        }

        private static (List<PageType> Used, List<PageType> Unused, Dictionary<string, List<string>> SiteMap) ClassifyPageTypes(KX13ProjectContent model)
        {
            var siteMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var treeClassNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var site in model.Sites)
            {
                foreach (var pt in site.AssignedPageTypes)
                {
                    if (pt.ClassName is null) continue;
                    if (!siteMap.ContainsKey(pt.ClassName))
                        siteMap[pt.ClassName] = new List<string>();
                    if (site.SiteName is not null)
                        siteMap[pt.ClassName].Add(site.SiteName);
                }

                foreach (var node in FlattenTree(site.ContentTree))
                {
                    if (node.PageTypeClassName is not null)
                        treeClassNames.Add(node.PageTypeClassName);
                }
            }

            var used = new List<PageType>();
            var unused = new List<PageType>();

            foreach (var pt in model.AllPageTypes)
            {
                if (pt.ClassName is not null && treeClassNames.Contains(pt.ClassName))
                    used.Add(pt);
                else
                    unused.Add(pt);
            }

            return (used, unused, siteMap);
        }

        private static Dictionary<string, List<(string SiteName, string Path)>> BuildUsageIndex(List<Site> sites)
        {
            var index = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var site in sites)
            {
                foreach (var node in FlattenTree(site.ContentTree))
                {
                    if (node.PageTypeClassName is null || node.NodeAliasPath is null) continue;

                    if (!index.ContainsKey(node.PageTypeClassName))
                        index[node.PageTypeClassName] = new List<(string, string)>();

                    index[node.PageTypeClassName].Add((site.SiteName ?? "Unknown", node.NodeAliasPath));
                }
            }

            return index;
        }

        private static string FormatUsageSummary(List<(string SiteName, string Path)> usages)
        {
            if (usages.Count <= 3)
            {
                return string.Join(", ", usages.Select(u => u.Path));
            }

            // Group by first path segment
            var groups = usages
                .GroupBy(u =>
                {
                    string path = u.Path.TrimStart('/');
                    int slash = path.IndexOf('/');
                    return slash > 0 ? "/" + path[..slash] : u.Path;
                })
                .OrderByDescending(g => g.Count())
                .Select(g => g.Count() > 3 ? $"{g.Key}/* ({g.Count()} pages)" : string.Join(", ", g.Select(x => x.Path)));

            return string.Join(", ", groups);
        }

        // ── Page Builder Components ────────────────────────

        private static void RenderPageBuilderComponentsSection(StringBuilder sb, List<PageBuilderComponentDefinition> catalogue)
        {
            sb.AppendLine("## Page Builder Components");
            sb.AppendLine();

            var templates = catalogue.Where(c => c.Kind == PageBuilderComponentKind.PageTemplate).ToList();
            var sections = catalogue.Where(c => c.Kind == PageBuilderComponentKind.Section).ToList();
            var widgets = catalogue.Where(c => c.Kind == PageBuilderComponentKind.Widget).ToList();

            // Summary counts
            sb.AppendLine($"- **Templates:** {templates.Count}");
            sb.AppendLine($"- **Sections:** {sections.Count}");
            sb.AppendLine($"- **Widgets:** {widgets.Count}");

            var pbPageTypes = catalogue
                .SelectMany(c => c.AllowedForPageTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x);
            var pbList = pbPageTypes.ToList();
            if (pbList.Count > 0)
            {
                sb.AppendLine($"- **Page Builder-enabled page types:** {string.Join(", ", pbList.Select(p => $"`{p}`"))}");
            }

            sb.AppendLine();

            RenderComponentKindTable(sb, "Page Templates", templates);
            RenderComponentKindTable(sb, "Sections", sections);
            RenderComponentKindWidgetTable(sb, "Widgets", widgets);
        }

        private static void RenderComponentKindTable(StringBuilder sb, string heading, List<PageBuilderComponentDefinition> components)
        {
            sb.AppendLine($"### {heading}");
            sb.AppendLine();

            if (components.Count == 0)
            {
                sb.AppendLine("_None discovered_");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Identifier | Properties | Used On Page Types |");
            sb.AppendLine("| ---------- | ---------- | ------------------ |");

            foreach (var comp in components.OrderBy(c => c.Identifier))
            {
                string props = comp.PropertyDefinitions.Count > 0
                    ? string.Join(", ", comp.PropertyDefinitions.OrderBy(p => p.Order).Select(p => $"{p.PropertyName} ({p.PropertyTypeName ?? "unknown"})"))
                    : "*(none)*";
                string pageTypes = comp.AllowedForPageTypes.Count > 0
                    ? string.Join(", ", comp.AllowedForPageTypes)
                    : "—";
                sb.AppendLine($"| {Esc(comp.Identifier)} | {props} | {pageTypes} |");
            }

            sb.AppendLine();
        }

        private static void RenderComponentKindWidgetTable(StringBuilder sb, string heading, List<PageBuilderComponentDefinition> components)
        {
            sb.AppendLine($"### {heading}");
            sb.AppendLine();

            if (components.Count == 0)
            {
                sb.AppendLine("_None discovered_");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Identifier | Properties | Used On Page Types |");
            sb.AppendLine("| ---------- | ---------- | ------------------ |");

            foreach (var comp in components.OrderBy(c => c.Identifier))
            {
                string props = comp.PropertyDefinitions.Count > 0
                    ? string.Join(", ", comp.PropertyDefinitions.OrderBy(p => p.Order).Select(p => $"{p.PropertyName} ({p.PropertyTypeName ?? "unknown"})"))
                    : "*(none)*";
                string pageTypes = comp.AllowedForPageTypes.Count > 0
                    ? string.Join(", ", comp.AllowedForPageTypes)
                    : "—";
                sb.AppendLine($"| {Esc(comp.Identifier)} | {props} | {pageTypes} |");
            }

            sb.AppendLine();
        }

        // ── Content Reference Chains ───────────────────────

        private static void RenderContentReferenceChains(StringBuilder sb, List<PageContentReferenceEntry> graph)
        {
            sb.AppendLine("## Content References");
            sb.AppendLine();

            if (graph.Count == 0)
            {
                sb.AppendLine("_No content references found_");
                sb.AppendLine();
                return;
            }

            // Group by source page type
            var grouped = graph
                .GroupBy(e => e.SourcePageTypeClassName ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach (var typeGroup in grouped)
            {
                sb.AppendLine($"#### {typeGroup.Key}");
                sb.AppendLine();

                // Sub-group by widget + property
                var byWidget = typeGroup
                    .GroupBy(e => new { Widget = e.WidgetTypeIdentifier ?? "(page type field)", Property = e.PropertyName ?? "unknown" })
                    .OrderBy(g => g.Key.Widget).ThenBy(g => g.Key.Property);

                foreach (var widgetGroup in byWidget)
                {
                    string widgetLabel = widgetGroup.Key.Widget == "(page type field)"
                        ? "Page Type Field References"
                        : "Widget References";
                    sb.AppendLine($"##### {widgetLabel}");
                    sb.AppendLine();

                    string header = widgetGroup.Key.Widget != "(page type field)"
                        ? $"**`{widgetGroup.Key.Widget}`** — property `{widgetGroup.Key.Property}` — {widgetGroup.First().ReferenceType} ({widgetGroup.Count()} references)"
                        : $"**property `{widgetGroup.Key.Property}`** — {widgetGroup.First().ReferenceType} ({widgetGroup.Count()} references)";
                    sb.AppendLine(header);
                    sb.AppendLine();

                    sb.AppendLine("| Source Path | Target Path / GUID | Target Page Type |");
                    sb.AppendLine("| ----------- | ------------------ | ---------------- |");

                    foreach (var entry in widgetGroup)
                    {
                        string target = entry.TargetNodeAliasPath
                            ?? entry.TargetNodeGuid?.ToString()
                            ?? entry.TargetMediaFileGuid?.ToString()
                            ?? "—";
                        string targetType = entry.TargetPageTypeClassName ?? (entry.TargetMediaFileGuid.HasValue ? "_(media file)_" : "—");
                        sb.AppendLine($"| {Esc(entry.SourceNodeAliasPath)} | {target} | {targetType} |");
                    }

                    sb.AppendLine();
                }
            }
        }

        // ── Relationships ─────────────────────────────────

        private static void RenderRelationshipsSection(
            StringBuilder sb,
            List<RelationshipName> relationshipNames,
            List<Relationship> relationships)
        {
            sb.AppendLine("## Relationships");
            sb.AppendLine();

            if (relationships.Count == 0)
            {
                sb.AppendLine("_No page relationships found_");
                sb.AppendLine();
                return;
            }

            int adHocRelationshipCount = relationships.Count(r => r.IsAdHoc);
            int namedRelationshipCount = relationships.Count - adHocRelationshipCount;
            int unresolvedAdHocNameCount = relationshipNames.Count(rn => rn.IsAdHoc && string.IsNullOrWhiteSpace(rn.SourceFieldName));

            sb.AppendLine($"- **Relationship rows:** {relationships.Count}");
            sb.AppendLine($"- **Relationship names:** {relationshipNames.Count}");
            sb.AppendLine($"- **Ad-hoc relationships:** {adHocRelationshipCount}");
            sb.AppendLine($"- **Named relationships:** {namedRelationshipCount}");
            sb.AppendLine($"- **Unresolved ad-hoc names:** {unresolvedAdHocNameCount}");
            sb.AppendLine();

            var relationshipNameLookup = relationshipNames.ToDictionary(rn => rn.RelationshipNameId);

            RenderAdHocRelationshipGroups(sb, relationships, relationshipNameLookup);
            RenderNamedRelationshipGroups(sb, relationships);
        }

        private static void RenderAdHocRelationshipGroups(
            StringBuilder sb,
            List<Relationship> relationships,
            Dictionary<int, RelationshipName> relationshipNameLookup)
        {
            var adHocRelationships = relationships.Where(r => r.IsAdHoc).ToList();
            if (adHocRelationships.Count == 0)
            {
                return;
            }

            sb.AppendLine("### Ad-hoc Relationships");
            sb.AppendLine();

            var groups = adHocRelationships
                .GroupBy(r =>
                {
                    if (relationshipNameLookup.TryGetValue(r.RelationshipNameId, out var relationshipName) &&
                        !string.IsNullOrWhiteSpace(relationshipName.SourcePageTypeClassName) &&
                        !string.IsNullOrWhiteSpace(relationshipName.SourceFieldName))
                    {
                        return $"{relationshipName.SourcePageTypeClassName}.{relationshipName.SourceFieldName}";
                    }

                    return "Unresolved ad-hoc relationships";
                })
                .OrderBy(g => g.Key == "Unresolved ad-hoc relationships" ? "zzz" : g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                sb.AppendLine($"#### {Esc(group.Key)}");
                sb.AppendLine();
                sb.AppendLine($"- **Relationship count:** {group.Count()}");

                if (group.Key == "Unresolved ad-hoc relationships")
                {
                    string unresolvedNames = string.Join(", ", group
                        .Select(r => r.RelationshipDisplayName ?? r.RelationshipName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3));

                    if (!string.IsNullOrWhiteSpace(unresolvedNames))
                    {
                        sb.AppendLine($"- **Example names:** {Esc(unresolvedNames)}");
                    }
                }

                foreach (string example in group
                    .Select(FormatRelationshipExample)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3))
                {
                    sb.AppendLine($"- {Esc(example)}");
                }

                sb.AppendLine();
            }
        }

        private static void RenderNamedRelationshipGroups(StringBuilder sb, List<Relationship> relationships)
        {
            var namedRelationships = relationships.Where(r => !r.IsAdHoc).ToList();
            if (namedRelationships.Count == 0)
            {
                return;
            }

            sb.AppendLine("### Named Relationships");
            sb.AppendLine();

            var groups = namedRelationships
                .GroupBy(r => r.RelationshipDisplayName ?? r.RelationshipName)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                sb.AppendLine($"#### {Esc(group.Key)}");
                sb.AppendLine();
                sb.AppendLine($"- **Relationship count:** {group.Count()}");

                foreach (string example in group
                    .Select(FormatRelationshipExample)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3))
                {
                    sb.AppendLine($"- {Esc(example)}");
                }

                sb.AppendLine();
            }
        }

        private static string FormatRelationshipExample(Relationship relationship)
        {
            string leftPath = relationship.LeftNodeAliasPath ?? "(unknown source)";
            string rightPath = relationship.RightNodeAliasPath ?? "(unknown target)";
            return $"{leftPath} -> {rightPath}";
        }

        // ── Custom Modules ─────────────────────────────────

        private static void RenderCustomModulesSection(StringBuilder sb, List<CustomModule> modules)
        {
            sb.AppendLine("## Custom Modules & Classes");
            sb.AppendLine();

            if (modules.Count == 0)
            {
                sb.AppendLine("_None_");
                sb.AppendLine();
                return;
            }

            foreach (var module in modules.OrderBy(m => m.ResourceName))
            {
                string devFlag = module.IsInDevelopment ? " _(in development)_" : "";
                sb.AppendLine($"### {Esc(module.ResourceDisplayName)} — _{Esc(module.ResourceName)}_{devFlag}");
                sb.AppendLine();

                foreach (var cls in module.Classes.OrderBy(c => c.ClassName))
                {
                    sb.AppendLine($"#### {Esc(cls.ClassName)} — _{Esc(cls.ClassDisplayName)}_");
                    sb.AppendLine();

                    string coupledTable = !string.IsNullOrEmpty(cls.ClassTableName)
                        ? $"`{cls.ClassTableName}`"
                        : "—";
                    sb.AppendLine($"- **Coupled table:** {coupledTable}");
                    sb.AppendLine($"- **Parent class:** {(string.IsNullOrEmpty(cls.ParentClassName) ? "—" : $"`{cls.ParentClassName}`")}");
                    sb.AppendLine();

                    RenderFieldTable(sb, cls.Fields);

                    if (cls.References.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("**References:**");
                        foreach (var r in cls.References)
                        {
                            sb.AppendLine($"- `{r.FieldName}` → `{r.TargetObjectType}` ({r.DependencyType})");
                        }
                    }

                    sb.AppendLine();
                }
            }
        }

        // ── Custom Tables ──────────────────────────────────

        private static void RenderCustomTablesSection(StringBuilder sb, List<CustomTable> tables)
        {
            sb.AppendLine("## Custom Tables");
            sb.AppendLine();

            if (tables.Count == 0)
            {
                sb.AppendLine("_None_");
                sb.AppendLine();
                return;
            }

            foreach (var table in tables.OrderBy(t => t.ClassName))
            {
                sb.AppendLine($"### {Esc(table.ClassName)} — _{Esc(table.ClassDisplayName)}_");
                sb.AppendLine();

                string coupledTable = !string.IsNullOrEmpty(table.ClassTableName)
                    ? $"`{table.ClassTableName}`"
                    : "—";
                sb.AppendLine($"- **Coupled table:** {coupledTable}");
                sb.AppendLine();

                RenderFieldTable(sb, table.Fields);
                sb.AppendLine();
            }
        }

        // ── Forms ──────────────────────────────────────────

        private static void RenderFormsSection(StringBuilder sb, List<Site> sites)
        {
            sb.AppendLine("## Forms");
            sb.AppendLine();

            bool anyForms = false;

            foreach (var site in sites)
            {
                foreach (var form in site.Forms.OrderBy(f => f.FormName))
                {
                    anyForms = true;
                    sb.AppendLine($"### {Esc(form.FormName)} — _{Esc(form.FormDisplayName)}_");
                    sb.AppendLine();

                    string tableName = !string.IsNullOrEmpty(form.FormTableName)
                        ? $"`{form.FormTableName}`"
                        : "—";
                    sb.AppendLine($"- **Table:** {tableName}");
                    sb.AppendLine($"- **Site:** {Esc(site.SiteDisplayName)}");
                    sb.AppendLine($"- **Submit action:** {(string.IsNullOrEmpty(form.DisplayText) ? "—" : Esc(form.DisplayText))}");
                    sb.AppendLine($"- **Notification email:** {(string.IsNullOrEmpty(form.SendToEmail) ? "—" : Esc(form.SendToEmail))}");
                    sb.AppendLine($"- **Confirmation email:** {(string.IsNullOrEmpty(form.ConfirmationSendFromEmail) ? "—" : Esc(form.ConfirmationSendFromEmail))}");
                    sb.AppendLine($"- **Log activity:** {(form.LogActivity ? "Yes" : "No")}");
                    sb.AppendLine();

                    RenderFormFieldTable(sb, form.Fields);
                    sb.AppendLine();
                }
            }

            if (!anyForms)
            {
                sb.AppendLine("_None_");
                sb.AppendLine();
            }
        }

        // ── Shared helpers ─────────────────────────────────

        private static string FormatType(FieldDefinition f)
        {
            string baseType = f.DataType ?? "unknown";
            if (f.Size.HasValue && f.Size.Value > 0)
            {
                return $"{baseType}({f.Size.Value})";
            }
            return baseType;
        }

        private static void RenderFieldTable(StringBuilder sb, List<FieldDefinition> fields)
        {
            if (fields.Count == 0)
            {
                sb.AppendLine("*No custom fields.*");
                return;
            }

            sb.AppendLine("| # | Field | Caption | Type | Required | Control / Component | Reference |");
            sb.AppendLine("| --- | ----- | ------- | ---- | -------- | ------------------- | --------- |");

            int i = 1;
            foreach (var f in fields.OrderBy(f => f.Order))
            {
                string required = f.IsRequired ? "Yes" : "No";
                string control = f.FormControlName ?? f.FormComponentIdentifier ?? "—";
                string reference = f.ReferenceToObjectType is not null
                    ? $"{f.ReferenceType} → {f.ReferenceToObjectType}"
                    : "—";
                sb.AppendLine($"| {i++} | {Esc(f.FieldName)} | {Esc(f.FieldCaption)} | {FormatType(f)} | {required} | {Esc(control)} | {Esc(reference)} |");
            }
        }

        private static void RenderFormFieldTable(StringBuilder sb, List<FormFieldDefinition> fields)
        {
            if (fields.Count == 0)
            {
                sb.AppendLine("*No fields.*");
                return;
            }

            sb.AppendLine("| # | Field | Caption | Type | Required | Admin Component | Live Site Component | Validation |");
            sb.AppendLine("| --- | ----- | ------- | ---- | -------- | --------------- | ------------------- | ---------- |");

            int i = 1;
            foreach (var f in fields.OrderBy(f => f.Order))
            {
                string required = f.IsRequired ? "Yes" : "No";
                string adminComponent = f.FormComponentIdentifier ?? f.FormControlName ?? "—";
                string liveSite = !string.IsNullOrEmpty(f.LiveSiteFormComponentIdentifier) ? f.LiveSiteFormComponentIdentifier : "—";
                string validation = !string.IsNullOrEmpty(f.ValidationRule) ? Esc(f.ValidationRule) : "—";
                sb.AppendLine($"| {i++} | {Esc(f.FieldName)} | {Esc(f.FieldCaption)} | {FormatType(f)} | {required} | {Esc(adminComponent)} | {liveSite} | {validation} |");
            }
        }

        private static List<ContentTreeNode> FlattenTree(List<ContentTreeNode> roots)
        {
            var result = new List<ContentTreeNode>();
            var stack = new Stack<ContentTreeNode>(roots);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                result.Add(node);

                foreach (var child in node.Children)
                    stack.Push(child);
            }

            return result;
        }

        private static int CountNodes(List<ContentTreeNode> roots) => FlattenTree(roots).Count;

        private static string Esc(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "—";
            return value.Replace("|", "\\|");
        }
    }
}

