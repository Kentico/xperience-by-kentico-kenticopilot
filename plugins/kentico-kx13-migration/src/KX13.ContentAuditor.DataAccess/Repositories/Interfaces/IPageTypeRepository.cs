using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface IPageTypeRepository
    {
        public Task<List<PageType>> GetAllPageTypesAsync(AuditFilterOptions? filter = null);
        public Task<List<PageType>> GetPageTypesForSiteAsync(int siteId);
    }
}
