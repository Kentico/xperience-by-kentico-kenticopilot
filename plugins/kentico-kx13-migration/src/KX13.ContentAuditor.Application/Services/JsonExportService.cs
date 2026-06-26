using System.Text.Json;

using KX13.ContentAuditor.Application.Models.Export;
using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Application.Services
{
    public class JsonExportService
    {
        public void ExportAll(KX13ProjectContent model, string outputDir, JsonSerializerOptions options)
        {
            ExportSites(model.Sites, outputDir, options);
            ExportPageTypes(model.AllPageTypes, model.Sites, outputDir, options);
            WriteJson(outputDir, "custom-tables.json", model.AllCustomTables, options);
            WriteJson(outputDir, "custom-modules.json", model.AllCustomModules, options);
            WriteJson(outputDir, "forms.json", model.AllForms, options);
            WriteJson(outputDir, "page-builder-components.json", model.PageBuilderComponentCatalogue, options);
            WriteJson(outputDir, "content-reference-graph.json", model.ContentReferenceGraph, options);
            ExportRelationships(model.RelationshipNames, model.Relationships, outputDir, options);
        }

        public void ExportSites(List<Site> sites, string outputDir, JsonSerializerOptions options)
        {
            var exportSites = sites.Select(s => new ExportSite
            {
                SiteId = s.SiteId,
                SiteDisplayName = s.SiteDisplayName,
                SiteName = s.SiteName,
                SiteDomainName = s.SiteDomainName,
                SiteDefaultCultureCode = s.SiteDefaultCultureCode,
                SiteCultures = s.SiteCultures,
                ContentTree = s.ContentTree,
                AssignedPageTypeClassNames = s.AssignedPageTypes
                    .Where(pt => pt.ClassName is not null)
                    .Select(pt => pt.ClassName!)
                    .ToList(),
                AssignedCustomTableClassNames = s.AssignedCustomTables
                    .Where(ct => ct.ClassName is not null)
                    .Select(ct => ct.ClassName!)
                    .ToList(),
                FormNames = s.Forms
                    .Where(f => f.FormName is not null)
                    .Select(f => f.FormName!)
                    .ToList()
            }).ToList();

            WriteJson(outputDir, "sites.json", exportSites, options);
        }

        public void ExportPageTypes(List<PageType> pageTypes, List<Site> sites, string outputDir, JsonSerializerOptions options)
        {
            var siteMap = BuildPageTypeSiteMap(sites);

            var exportPageTypes = pageTypes.Select(pt => new ExportPageType
            {
                ClassId = pt.ClassId,
                ClassName = pt.ClassName,
                ClassDisplayName = pt.ClassDisplayName,
                ClassTableName = pt.ClassTableName,
                HasCustomFields = pt.HasCustomFields,
                PageBuilderEnabled = pt.PageBuilderEnabled,
                UrlEnabled = pt.UrlEnabled,
                MetadataEnabled = pt.MetadataEnabled,
                NavigationItemEnabled = pt.NavigationItemEnabled,
                UrlPattern = pt.UrlPattern,
                InheritsFromClassName = pt.InheritsFromClassName,
                PageNameSourceField = pt.PageNameSourceField,
                Fields = pt.Fields,
                Sites = pt.ClassName is not null && siteMap.TryGetValue(pt.ClassName, out var siteNames)
                    ? siteNames
                    : new List<string>()
            }).ToList();

            WriteJson(outputDir, "page-types.json", exportPageTypes, options);
        }

        public void ExportCustomTables(List<CustomTable> tables, string outputDir, JsonSerializerOptions options) =>
            WriteJson(outputDir, "custom-tables.json", tables, options);

        public void ExportCustomModules(List<CustomModule> modules, string outputDir, JsonSerializerOptions options) =>
            WriteJson(outputDir, "custom-modules.json", modules, options);

        public void ExportForms(List<Form> forms, string outputDir, JsonSerializerOptions options) =>
            WriteJson(outputDir, "forms.json", forms, options);

        public void ExportPageBuilderComponents(List<PageBuilderComponentDefinition> components, string outputDir, JsonSerializerOptions options) =>
            WriteJson(outputDir, "page-builder-components.json", components, options);

        public void ExportRelationships(
            List<RelationshipName> relationshipNames,
            List<Relationship> relationships,
            string outputDir,
            JsonSerializerOptions options) =>
            WriteJson(outputDir, "relationships.json", new ExportRelationships
            {
                RelationshipNames = relationshipNames,
                Relationships = relationships
            }, options);

        public void ExportFailures(List<AuditFailure> failures, string outputDir, JsonSerializerOptions options) =>
            WriteJson(outputDir, "failures.json", failures, options);

        private static Dictionary<string, List<string>> BuildPageTypeSiteMap(List<Site> sites)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var site in sites)
            {
                foreach (var pt in site.AssignedPageTypes)
                {
                    if (pt.ClassName is null) continue;
                    if (!map.ContainsKey(pt.ClassName))
                        map[pt.ClassName] = new List<string>();
                    if (site.SiteName is not null)
                        map[pt.ClassName].Add(site.SiteName);
                }
            }

            return map;
        }

        private static void WriteJson<T>(string outputDir, string fileName, T data, JsonSerializerOptions options)
        {
            string path = Path.Combine(outputDir, fileName);
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
            Console.WriteLine($"  -> {fileName}");
        }
    }
}
