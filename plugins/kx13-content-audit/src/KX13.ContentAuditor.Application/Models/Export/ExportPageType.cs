using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Application.Models.Export
{
    public class ExportPageType
    {
        public int ClassId { get; set; }

        public string? ClassName { get; set; }

        public string? ClassDisplayName { get; set; }

        public string? ClassTableName { get; set; }

        public bool HasCustomFields { get; set; }

        public bool PageBuilderEnabled { get; set; }

        public bool UrlEnabled { get; set; }

        public bool MetadataEnabled { get; set; }

        public bool NavigationItemEnabled { get; set; }

        public string? UrlPattern { get; set; }

        public string? InheritsFromClassName { get; set; }

        public string? PageNameSourceField { get; set; }

        public List<FieldDefinition> Fields { get; set; } = new();

        public List<string> Sites { get; set; } = new();
    }
}
