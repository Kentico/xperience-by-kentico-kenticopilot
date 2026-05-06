namespace KX13.ContentAuditor.DataAccess.Models
{
    public class Site
    {
        public int SiteId { get; set; }

        public string? SiteDisplayName { get; set; }

        public string? SiteName { get; set; }

        public string? SiteDomainName { get; set; }

        public string? SiteDefaultCultureCode { get; set; }

        public List<string> SiteCultures { get; set; } = [];

        public List<ContentTreeNode> ContentTree { get; set; } = [];

        public List<PageType> AssignedPageTypes { get; set; } = [];

        public List<CustomTable> AssignedCustomTables { get; set; } = [];
        public List<Form> Forms { get; set; } = [];
    }
}
