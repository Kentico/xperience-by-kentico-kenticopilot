using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface ISiteRepository
    {
        public Task<List<Site>> GetSitesAsync(AuditFilterOptions? filter = null);
        public Task<List<string>> GetSiteCulturesAsync(int siteId);
    }
}
