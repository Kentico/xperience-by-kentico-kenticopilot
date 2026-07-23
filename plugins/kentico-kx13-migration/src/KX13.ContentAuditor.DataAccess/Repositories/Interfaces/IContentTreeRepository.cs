using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface IContentTreeRepository
    {
        public Task<List<ContentTreeNode>> GetContentTreeAsync(int siteId, AuditFilterOptions? filter = null, string? culture = null);
        public Task<List<ContentTreeNode>> GetNodesByIdsAsync(IEnumerable<int> nodeIds);
        public Task<List<(int NodeId, Dictionary<string, object?> FieldValues)>> GetCoupledDataForSiteNodesAsync(
            int siteId,
            string tableName,
            string primaryKeyColumn,
            string? culture = null);
    }
}
