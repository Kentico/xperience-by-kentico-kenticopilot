namespace KX13.ContentAuditor.DataAccess.Models
{
    public class Relationship
    {
        public int RelationshipId { get; set; }

        public int SiteId { get; set; }

        public string? SiteName { get; set; }

        public int RelationshipNameId { get; set; }

        public string RelationshipName { get; set; } = string.Empty;

        public string? RelationshipDisplayName { get; set; }

        public bool IsAdHoc { get; set; }

        public int LeftNodeId { get; set; }

        public string? LeftNodeAliasPath { get; set; }

        public string? LeftClassName { get; set; }

        public int RightNodeId { get; set; }

        public string? RightNodeAliasPath { get; set; }

        public string? RightClassName { get; set; }

        public int Order { get; set; }
    }
}
