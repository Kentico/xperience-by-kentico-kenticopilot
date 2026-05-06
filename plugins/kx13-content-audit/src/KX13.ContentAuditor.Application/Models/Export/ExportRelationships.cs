using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Application.Models.Export
{
    public class ExportRelationships
    {
        public List<RelationshipName> RelationshipNames { get; set; } = new();

        public List<Relationship> Relationships { get; set; } = new();
    }
}
