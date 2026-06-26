using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface IRelationshipRepository
    {
        public Task<List<RelationshipName>> GetRelationshipNamesAsync();

        public Task<List<Relationship>> GetRelationshipsForSiteAsync(int siteId);
    }
}
