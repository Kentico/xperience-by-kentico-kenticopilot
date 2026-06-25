using System.Text.Json;

using KX13.ContentAuditor.Application.Services;
using KX13.ContentAuditor.DataAccess;
using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.CLI
{
    internal sealed class AuditCliRunner
    {
        private readonly ContentModelService contentModelService;
        private readonly JsonExportService jsonExportService;
        private readonly MarkdownReportService reportService;
        private readonly AuditFailureCollector failureCollector;

        public AuditCliRunner(
            ContentModelService contentModelService,
            JsonExportService jsonExportService,
            MarkdownReportService reportService,
            AuditFailureCollector failureCollector)
        {
            this.contentModelService = contentModelService;
            this.jsonExportService = jsonExportService;
            this.reportService = reportService;
            this.failureCollector = failureCollector;
        }

        public async Task ExecuteAsync(AuditCliOptions options)
        {
            string outputDir = options.OutputPath ?? GetDefaultOutputDir();
            Directory.CreateDirectory(outputDir);

            AuditFilterOptions filter = options.ToFilterOptions();
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Console.WriteLine($"Output directory: {Path.GetFullPath(outputDir)}");

            if (filter.HasAnyFilter)
            {
                Console.WriteLine("Active filters:");
                if (filter.HasSiteFilter)
                {
                    Console.WriteLine($"  Site name:  {filter.SiteName}");
                }

                if (filter.HasClassNameFilter)
                {
                    Console.WriteLine($"  Class name: {filter.ClassNamePattern}");
                }

                if (filter.HasPagePathFilter)
                {
                    Console.WriteLine($"  Page path:  {filter.PagePathPrefix}");
                }
            }

            Console.WriteLine();

            if (options.ExportAll)
            {
                Console.WriteLine("Exporting full content model...");
                KX13ProjectContent model = await contentModelService.BuildFullContentModelAsync(filter);

                jsonExportService.ExportAll(model, outputDir, jsonOptions);

                Console.WriteLine("Generating content model report...");
                string report = reportService.GenerateReport(model);
                File.WriteAllText(Path.Combine(outputDir, "content-model-report.md"), report);
                Console.WriteLine("  -> content-model-report.md");
            }
            else
            {
                KX13ProjectContent? fullModel = null;

                if (options.GenerateReport)
                {
                    Console.WriteLine("Building full content model for report...");
                    fullModel = await contentModelService.BuildFullContentModelAsync(filter);
                }

                if (options.ExportSites)
                {
                    Console.WriteLine("Exporting sites...");
                    var sites = fullModel?.Sites ?? await contentModelService.BuildSitesAsync(filter);
                    jsonExportService.ExportSites(sites, outputDir, jsonOptions);
                }

                if (options.ExportPageTypes)
                {
                    Console.WriteLine("Exporting page types...");
                    if (fullModel is not null)
                    {
                        jsonExportService.ExportPageTypes(fullModel.AllPageTypes, fullModel.Sites, outputDir, jsonOptions);
                    }
                    else
                    {
                        var pageTypes = await contentModelService.BuildPageTypesAsync(filter);
                        var sites = await contentModelService.BuildSitesAsync(filter);
                        jsonExportService.ExportPageTypes(pageTypes, sites, outputDir, jsonOptions);
                    }
                }

                if (options.ExportCustomTables)
                {
                    Console.WriteLine("Exporting custom tables...");
                    var tables = fullModel?.AllCustomTables ?? await contentModelService.BuildCustomTablesAsync();
                    jsonExportService.ExportCustomTables(tables, outputDir, jsonOptions);
                }

                if (options.ExportCustomModules)
                {
                    Console.WriteLine("Exporting custom modules...");
                    var modules = fullModel?.AllCustomModules ?? await contentModelService.BuildCustomModulesAsync();
                    jsonExportService.ExportCustomModules(modules, outputDir, jsonOptions);
                }

                if (options.ExportForms)
                {
                    Console.WriteLine("Exporting forms...");
                    var forms = fullModel?.AllForms ?? await contentModelService.BuildFormsAsync();
                    jsonExportService.ExportForms(forms, outputDir, jsonOptions);
                }

                if (options.ExportPageBuilderComponents)
                {
                    Console.WriteLine("Exporting page builder components...");
                    var components = fullModel?.PageBuilderComponentCatalogue ?? await contentModelService.BuildPageBuilderComponentsAsync(filter);
                    jsonExportService.ExportPageBuilderComponents(components, outputDir, jsonOptions);
                }

                if (options.ExportRelationships)
                {
                    Console.WriteLine("Exporting relationships...");
                    var relationshipModel = fullModel ?? await contentModelService.BuildRelationshipsAsync(filter);
                    jsonExportService.ExportRelationships(relationshipModel.RelationshipNames, relationshipModel.Relationships, outputDir, jsonOptions);
                }

                if (options.GenerateReport && fullModel is not null)
                {
                    Console.WriteLine("Generating content model report...");
                    string report = reportService.GenerateReport(fullModel);
                    File.WriteAllText(Path.Combine(outputDir, "content-model-report.md"), report);
                    Console.WriteLine("  -> content-model-report.md");
                }
            }

            if (options.HasJsonExport)
            {
                List<AuditFailure> failures = failureCollector.GetFailures();
                if (failures.Count > 0)
                {
                    Console.WriteLine("Exporting failures...");
                    jsonExportService.ExportFailures(failures, outputDir, jsonOptions);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done.");
        }

        private static string GetDefaultOutputDir()
        {
            string? projectRoot = FindProjectRoot(AppContext.BaseDirectory);
            return projectRoot is not null
                ? Path.Combine(projectRoot, "audit-results")
                : Path.GetFullPath("audit-results");
        }

        private static string? FindProjectRoot(string startPath)
        {
            var current = new DirectoryInfo(startPath);

            while (current is not null)
            {
                bool hasSolution = File.Exists(Path.Combine(current.FullName, "src", "KX13.ContentAuditor.slnx"));
                if (hasSolution)
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}