using System.Text.Json.Serialization;

namespace KX13.ContentAuditor.DataAccess.Models
{
    public class ContentTreeNode
    {
        public int NodeId { get; set; }

        public Guid NodeGuid { get; set; }

        public int? NodeParentId { get; set; }

        public string? NodeAliasPath { get; set; }

        public int? NodeLinkedNodeId { get; set; }

        public int? NodeLinkedNodeSiteId { get; set; }

        public string? DocumentName { get; set; }

        public string? PageTypeClassName { get; set; }

        [JsonIgnore]
        public string? LinkedOriginalNodeAliasPath { get; set; }

        [JsonIgnore]
        public string? LinkedOriginalClassName { get; set; }

        [JsonIgnore]
        public PageType? PageType { get; set; }

        public List<ContentTreeNode> Children { get; set; } = [];

        public PageBuilderConfiguration? PageBuilderConfig { get; set; }

        public Dictionary<string, object?> CustomFieldValues { get; set; } = [];

        public List<ContentReference> PageTypeFieldReferences { get; set; } = [];
    }
}
