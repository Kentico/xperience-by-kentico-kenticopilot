using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.CLI
{
    internal sealed class AuditCliOptions
    {
        public List<string> Errors { get; init; } = [];

        public bool ShowHelp { get; init; }

        public bool ExportSites { get; init; }

        public bool ExportPageTypes { get; init; }

        public bool ExportPageBuilderComponents { get; init; }

        public bool ExportCustomModules { get; init; }

        public bool ExportCustomTables { get; init; }

        public bool ExportForms { get; init; }

        public bool ExportRelationships { get; init; }

        public bool GenerateReport { get; init; }

        public string? OutputPath { get; init; }

        public string? SiteName { get; init; }

        public string? ClassNamePattern { get; init; }

        public string? PagePathPrefix { get; init; }

        public bool ExportAll => !ExportSites
            && !ExportPageTypes
            && !ExportPageBuilderComponents
            && !ExportCustomModules
            && !ExportCustomTables
            && !ExportForms
            && !ExportRelationships
            && !GenerateReport;

        public bool HasJsonExport => ExportAll
            || ExportSites
            || ExportPageTypes
            || ExportPageBuilderComponents
            || ExportCustomModules
            || ExportCustomTables
            || ExportForms
            || ExportRelationships;

        public AuditFilterOptions ToFilterOptions() => new()
        {
            SiteName = SiteName,
            ClassNamePattern = ClassNamePattern,
            PagePathPrefix = PagePathPrefix
        };
    }
}