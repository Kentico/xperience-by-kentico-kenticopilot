namespace KX13.ContentAuditor.DataAccess.Models
{
    public class ModuleClass
    {
        public int ClassId { get; set; }

        public string? ClassName { get; set; }

        public string? ClassDisplayName { get; set; }

        public string? ClassTableName { get; set; }

        public string? ParentClassName { get; set; }

        public List<FieldDefinition> Fields { get; set; } = [];

        public List<ModuleClassReference> References { get; set; } = [];
    }
}
