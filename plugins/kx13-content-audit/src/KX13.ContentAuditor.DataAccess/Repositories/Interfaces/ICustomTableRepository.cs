using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface ICustomTableRepository
    {
        public Task<List<CustomTable>> GetAllCustomTablesAsync();
        public Task<List<CustomTable>> GetCustomTablesForSiteAsync(int siteId);
        public Task<List<AlternativeForm>> GetAlternativeFormsAsync(int classId);
    }
}
