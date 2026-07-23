using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Application.Models.Export
{
    public class ExportSite
    {
        public int SiteId { get; set; }

        public string? SiteDisplayName { get; set; }

        public string? SiteName { get; set; }

        public string? SiteDomainName { get; set; }

        public string? SiteDefaultCultureCode { get; set; }

        public List<string> SiteCultures { get; set; } = new();

        public List<ContentTreeNode> ContentTree { get; set; } = new();

        public List<string> AssignedPageTypeClassNames { get; set; } = new();

        public List<string> AssignedCustomTableClassNames { get; set; } = new();

        public List<string> FormNames { get; set; } = new();
    }
}
