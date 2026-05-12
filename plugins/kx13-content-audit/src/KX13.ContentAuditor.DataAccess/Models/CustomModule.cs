namespace KX13.ContentAuditor.DataAccess.Models
{
    public class CustomModule
    {
        public int ResourceId { get; set; }

        public string? ResourceDisplayName { get; set; }

        public string? ResourceName { get; set; }

        public string? ResourceDescription { get; set; }

        public bool IsInDevelopment { get; set; }

        public List<ModuleClass> Classes { get; set; } = [];
    }
}
