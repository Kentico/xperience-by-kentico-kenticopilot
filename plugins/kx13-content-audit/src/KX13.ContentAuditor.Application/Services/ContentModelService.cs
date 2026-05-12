using KX13.ContentAuditor.DataAccess;
using KX13.ContentAuditor.DataAccess.Analysis;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Repositories;

namespace KX13.ContentAuditor.Application.Services
{
    public class ContentModelService
    {
        private readonly AuditFailureCollector failureCollector;
        private readonly ISiteRepository siteRepository;
        private readonly IPageTypeRepository pageTypeRepository;
        private readonly IContentTreeRepository contentTreeRepository;
        private readonly ICustomTableRepository customTableRepository;
        private readonly IFormRepository formRepository;
        private readonly ICustomModuleRepository customModuleRepository;
        private readonly IRelationshipRepository relationshipRepository;
        private readonly PageBuilderComponentDiscovery componentDiscovery;
        private readonly ContentReferenceAnalyzer referenceAnalyzer;

        public ContentModelService(
            AuditFailureCollector failureCollector,
            ISiteRepository siteRepository,
            IPageTypeRepository pageTypeRepository,
            IContentTreeRepository contentTreeRepository,
            ICustomTableRepository customTableRepository,
            IFormRepository formRepository,
            ICustomModuleRepository customModuleRepository,
            IRelationshipRepository relationshipRepository,
            PageBuilderComponentDiscovery componentDiscovery,
            ContentReferenceAnalyzer referenceAnalyzer)
        {
            this.failureCollector = failureCollector;
            this.siteRepository = siteRepository;
            this.pageTypeRepository = pageTypeRepository;
            this.contentTreeRepository = contentTreeRepository;
            this.customTableRepository = customTableRepository;
            this.formRepository = formRepository;
            this.customModuleRepository = customModuleRepository;
            this.relationshipRepository = relationshipRepository;
            this.componentDiscovery = componentDiscovery;
            this.referenceAnalyzer = referenceAnalyzer;
        }

        public async Task<KX13ProjectContent> BuildFullContentModelAsync(AuditFilterOptions? filter = null)
        {
            var model = new KX13ProjectContent();

            var allPageTypes = await pageTypeRepository.GetAllPageTypesAsync(filter);
            var relationshipResolutionPageTypes = filter?.HasClassNameFilter == true
                ? await pageTypeRepository.GetAllPageTypesAsync()
                : allPageTypes;
            var pageTypeLookup = allPageTypes
                .Where(pt => pt.ClassName is not null)
                .ToDictionary(pt => pt.ClassName!, StringComparer.OrdinalIgnoreCase);

            model.AllPageTypes = allPageTypes;
            model.AllCustomTables = await customTableRepository.GetAllCustomTablesAsync();
            model.AllCustomModules = await customModuleRepository.GetCustomModulesAsync();
            model.AllForms = await formRepository.GetAllFormsAsync();

            var sites = await siteRepository.GetSitesAsync(filter);
            var allNodes = new List<ContentTreeNode>();

            foreach (var site in sites)
            {
                await PopulateSiteAsync(site, pageTypeLookup, filter);
                allNodes.AddRange(FlattenTree(site.ContentTree));
            }

            model.Sites = sites;
            model.PageBuilderComponentCatalogue = componentDiscovery.DiscoverComponents(allNodes);
            model.ContentReferenceGraph = referenceAnalyzer.BuildReferenceGraph(allNodes);
            await PopulateRelationshipsAsync(model, sites, relationshipResolutionPageTypes);
            model.Failures = failureCollector.GetFailures();

            return model;
        }

        public async Task<List<Site>> BuildSitesAsync(AuditFilterOptions? filter = null)
        {
            var allPageTypes = await pageTypeRepository.GetAllPageTypesAsync(filter);
            var pageTypeLookup = allPageTypes
                .Where(pt => pt.ClassName is not null)
                .ToDictionary(pt => pt.ClassName!, StringComparer.OrdinalIgnoreCase);

            var sites = await siteRepository.GetSitesAsync(filter);

            foreach (var site in sites)
            {
                await PopulateSiteAsync(site, pageTypeLookup, filter);
            }

            return sites;
        }

        public Task<List<PageType>> BuildPageTypesAsync(AuditFilterOptions? filter = null) =>
            pageTypeRepository.GetAllPageTypesAsync(filter);

        public Task<List<CustomTable>> BuildCustomTablesAsync() => customTableRepository.GetAllCustomTablesAsync();

        public Task<List<CustomModule>> BuildCustomModulesAsync() => customModuleRepository.GetCustomModulesAsync();

        public Task<List<Form>> BuildFormsAsync() => formRepository.GetAllFormsAsync();

        public async Task<KX13ProjectContent> BuildRelationshipsAsync(AuditFilterOptions? filter = null)
        {
            var model = new KX13ProjectContent();
            var sites = await siteRepository.GetSitesAsync(filter);
            var pageTypes = await pageTypeRepository.GetAllPageTypesAsync();
            await PopulateRelationshipsAsync(model, sites, pageTypes);
            model.Failures = failureCollector.GetFailures();
            return model;
        }

        public async Task<List<PageBuilderComponentDefinition>> BuildPageBuilderComponentsAsync(AuditFilterOptions? filter = null)
        {
            var sites = await siteRepository.GetSitesAsync(filter);
            var allNodes = new List<ContentTreeNode>();

            foreach (var site in sites)
            {
                var flatNodes = await contentTreeRepository.GetContentTreeAsync(site.SiteId, filter, site.SiteDefaultCultureCode);
                allNodes.AddRange(flatNodes);
            }

            return componentDiscovery.DiscoverComponents(allNodes);
        }

        public async Task<KX13ProjectContent> BuildPageTypesAndContentTreeAsync(AuditFilterOptions? filter = null)
        {
            var model = new KX13ProjectContent();

            var allPageTypes = await pageTypeRepository.GetAllPageTypesAsync(filter);
            var pageTypeLookup = allPageTypes
                .Where(pt => pt.ClassName is not null)
                .ToDictionary(pt => pt.ClassName!, StringComparer.OrdinalIgnoreCase);

            model.AllPageTypes = allPageTypes;

            var sites = await siteRepository.GetSitesAsync(filter);

            foreach (var site in sites)
            {
                site.SiteCultures = await siteRepository.GetSiteCulturesAsync(site.SiteId);
                site.AssignedPageTypes = await pageTypeRepository.GetPageTypesForSiteAsync(site.SiteId);

                string? culture = site.SiteDefaultCultureCode;
                var flatNodes = await contentTreeRepository.GetContentTreeAsync(site.SiteId, filter, culture);
                LinkPageTypes(flatNodes, pageTypeLookup);
                await ResolveLinkedNodesAsync(flatNodes);
                site.ContentTree = BuildTree(flatNodes);
            }

            model.Sites = sites;
            model.Failures = failureCollector.GetFailures();

            return model;
        }

        private async Task PopulateSiteAsync(Site site, Dictionary<string, PageType> pageTypeLookup, AuditFilterOptions? filter)
        {
            site.SiteCultures = await siteRepository.GetSiteCulturesAsync(site.SiteId);
            site.AssignedPageTypes = await pageTypeRepository.GetPageTypesForSiteAsync(site.SiteId);

            string? culture = site.SiteDefaultCultureCode;
            var flatNodes = await contentTreeRepository.GetContentTreeAsync(site.SiteId, filter, culture);
            LinkPageTypes(flatNodes, pageTypeLookup);
            await ResolveLinkedNodesAsync(flatNodes);
            await LoadCoupledDataAsync(site.SiteId, flatNodes, pageTypeLookup, culture);
            AnalyzeFieldReferences(flatNodes, pageTypeLookup);
            site.ContentTree = BuildTree(flatNodes);

            site.AssignedCustomTables = await customTableRepository.GetCustomTablesForSiteAsync(site.SiteId);
            site.Forms = await formRepository.GetSiteFormsAsync(site.SiteId);
        }

        private async Task PopulateRelationshipsAsync(
            KX13ProjectContent model,
            List<Site> sites,
            List<PageType> pageTypes)
        {
            var relationshipNames = await relationshipRepository.GetRelationshipNamesAsync();
            var relationships = new List<Relationship>();

            foreach (var site in sites)
            {
                var siteRelationships = await relationshipRepository.GetRelationshipsForSiteAsync(site.SiteId);

                foreach (var relationship in siteRelationships)
                {
                    relationship.SiteId = site.SiteId;
                    relationship.SiteName = site.SiteName;
                }

                relationships.AddRange(siteRelationships);
            }

            relationships = relationships
                .OrderBy(r => r.SiteName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.LeftClassName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.RelationshipName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Order)
                .ToList();

            var usedRelationshipNameIds = relationships
                .Select(r => r.RelationshipNameId)
                .ToHashSet();
            var adHocRelationshipNameIds = relationships
                .Where(r => r.IsAdHoc)
                .Select(r => r.RelationshipNameId)
                .ToHashSet();
            var relevantRelationshipNames = relationshipNames
                .Where(rn => usedRelationshipNameIds.Contains(rn.RelationshipNameId))
                .OrderBy(rn => rn.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var fieldGuidLookup = BuildFieldGuidLookup(pageTypes);

            ResolveRelationshipNames(relevantRelationshipNames, adHocRelationshipNameIds, fieldGuidLookup);

            model.Relationships = relationships;
            model.RelationshipNames = relevantRelationshipNames;
        }

        private static Dictionary<Guid, List<(string ClassName, string FieldName)>> BuildFieldGuidLookup(List<PageType> pageTypes)
        {
            var lookup = new Dictionary<Guid, List<(string ClassName, string FieldName)>>();

            foreach (var pageType in pageTypes)
            {
                if (string.IsNullOrWhiteSpace(pageType.ClassName))
                {
                    continue;
                }

                foreach (var field in pageType.Fields)
                {
                    if (!field.FieldGuid.HasValue || string.IsNullOrWhiteSpace(field.FieldName))
                    {
                        continue;
                    }

                    if (!lookup.TryGetValue(field.FieldGuid.Value, out var matches))
                    {
                        matches = [];
                        lookup[field.FieldGuid.Value] = matches;
                    }

                    matches.Add((pageType.ClassName, field.FieldName));
                }
            }

            return lookup;
        }

        private static void ResolveRelationshipNames(
            List<RelationshipName> relationshipNames,
            HashSet<int> adHocRelationshipNameIds,
            Dictionary<Guid, List<(string ClassName, string FieldName)>> fieldGuidLookup)
        {
            foreach (var relationshipName in relationshipNames)
            {
                relationshipName.IsAdHoc = adHocRelationshipNameIds.Contains(relationshipName.RelationshipNameId);
                relationshipName.SourceFieldGuid = TryParseSourceFieldGuid(relationshipName.Name);

                if (!relationshipName.IsAdHoc ||
                    !relationshipName.SourceFieldGuid.HasValue ||
                    !fieldGuidLookup.TryGetValue(relationshipName.SourceFieldGuid.Value, out var matches) ||
                    matches.Count != 1)
                {
                    continue;
                }

                relationshipName.SourcePageTypeClassName = matches[0].ClassName;
                relationshipName.SourceFieldName = matches[0].FieldName;
            }
        }

        private static Guid? TryParseSourceFieldGuid(string? relationshipName)
        {
            if (string.IsNullOrWhiteSpace(relationshipName))
            {
                return null;
            }

            int separatorIndex = relationshipName.LastIndexOf('_');
            if (separatorIndex < 0 || separatorIndex == relationshipName.Length - 1)
            {
                return null;
            }

            return Guid.TryParse(relationshipName[(separatorIndex + 1)..], out Guid fieldGuid)
                ? fieldGuid
                : null;
        }

        private static void LinkPageTypes(List<ContentTreeNode> nodes, Dictionary<string, PageType> pageTypeLookup)
        {
            foreach (var node in nodes)
            {
                if (node.PageTypeClassName is not null &&
                    pageTypeLookup.TryGetValue(node.PageTypeClassName, out var pageType))
                {
                    node.PageType = pageType;
                }
            }
        }

        private async Task LoadCoupledDataAsync(int siteId, List<ContentTreeNode> nodes, Dictionary<string, PageType> pageTypeLookup, string? culture)
        {
            var classNames = nodes
                .Where(n => n.PageTypeClassName is not null)
                .Select(n => n.PageTypeClassName!)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var nodeLookup = nodes.ToLookup(n => n.NodeId);

            foreach (string className in classNames)
            {
                if (!pageTypeLookup.TryGetValue(className, out var pageType) ||
                    string.IsNullOrEmpty(pageType.ClassTableName))
                {
                    continue;
                }

                string tableName = pageType.ClassTableName;
                string pkColumn = GetCoupledTablePrimaryKey(tableName);

                var coupledData = await contentTreeRepository.GetCoupledDataForSiteNodesAsync(siteId, tableName, pkColumn, culture);

                foreach (var (nodeId, data) in coupledData)
                {
                    foreach (var node in nodeLookup[nodeId])
                    {
                        node.CustomFieldValues = data;
                    }
                }
            }
        }

        private void AnalyzeFieldReferences(List<ContentTreeNode> nodes, Dictionary<string, PageType> pageTypeLookup)
        {
            foreach (var node in nodes)
            {
                if (node.PageTypeClassName is null ||
                    !pageTypeLookup.TryGetValue(node.PageTypeClassName, out var pageType))
                {
                    continue;
                }

                node.PageTypeFieldReferences = referenceAnalyzer.AnalyzeFieldReferences(
                    pageType.Fields,
                    node.CustomFieldValues);
            }
        }

        private async Task ResolveLinkedNodesAsync(List<ContentTreeNode> flatNodes)
        {
            var nodeLookup = flatNodes.ToDictionary(node => node.NodeId);
            var unresolvedLinkedNodeIds = new HashSet<int>();

            foreach (var node in flatNodes)
            {
                if (!node.NodeLinkedNodeId.HasValue)
                {
                    continue;
                }

                if (nodeLookup.TryGetValue(node.NodeLinkedNodeId.Value, out var originalNode))
                {
                    AnnotateLinkedOriginal(node, originalNode);
                }
                else
                {
                    unresolvedLinkedNodeIds.Add(node.NodeLinkedNodeId.Value);
                }
            }

            if (unresolvedLinkedNodeIds.Count == 0)
            {
                return;
            }

            var originalNodes = await contentTreeRepository.GetNodesByIdsAsync(unresolvedLinkedNodeIds);
            var originalLookup = originalNodes.ToDictionary(node => node.NodeId);

            foreach (var node in flatNodes)
            {
                if (!node.NodeLinkedNodeId.HasValue ||
                    node.LinkedOriginalNodeAliasPath is not null ||
                    node.LinkedOriginalClassName is not null)
                {
                    continue;
                }

                if (originalLookup.TryGetValue(node.NodeLinkedNodeId.Value, out var originalNode))
                {
                    AnnotateLinkedOriginal(node, originalNode);
                }
            }
        }

        private static void AnnotateLinkedOriginal(ContentTreeNode linkedNode, ContentTreeNode originalNode)
        {
            linkedNode.LinkedOriginalNodeAliasPath = originalNode.NodeAliasPath;
            linkedNode.LinkedOriginalClassName = originalNode.PageTypeClassName;
        }

        private static List<ContentTreeNode> BuildTree(List<ContentTreeNode> flatNodes)
        {
            var lookup = new Dictionary<int, ContentTreeNode>();

            foreach (var node in flatNodes)
            {
                lookup[node.NodeId] = node;
            }

            var roots = new List<ContentTreeNode>();

            foreach (var node in flatNodes)
            {
                if (node.NodeParentId.HasValue && lookup.TryGetValue(node.NodeParentId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
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
                {
                    stack.Push(child);
                }
            }

            return result;
        }

        private static string GetCoupledTablePrimaryKey(string tableName)
        {
            // KX13 convention: coupled table PK = last segment of table name + "ID"
            // e.g., "DancingGoat_Article" -> "ArticleID", "CMS_MenuItem" -> "MenuItemID"
            int underscoreIndex = tableName.LastIndexOf('_');

            string suffix = underscoreIndex >= 0 && underscoreIndex < tableName.Length - 1
                ? tableName[(underscoreIndex + 1)..]
                : tableName;

            return $"{suffix}ID";
        }
    }
}
