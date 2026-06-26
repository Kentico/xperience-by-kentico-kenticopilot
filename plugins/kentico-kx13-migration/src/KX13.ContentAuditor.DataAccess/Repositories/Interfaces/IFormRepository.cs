using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface IFormRepository
    {
        public Task<List<Form>> GetSiteFormsAsync(int siteId);
        public Task<List<Form>> GetAllFormsAsync();
        public Task<List<AlternativeForm>> GetAlternativeFormsAsync(int classId);
    }
}
