using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public interface ICustomModuleRepository
    {
        public Task<List<CustomModule>> GetCustomModulesAsync();
        public Task<List<ModuleClass>> GetModuleClassesAsync(int resourceId);
    }
}
