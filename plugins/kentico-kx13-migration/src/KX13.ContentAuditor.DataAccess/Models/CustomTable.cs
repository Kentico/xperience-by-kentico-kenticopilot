namespace KX13.ContentAuditor.DataAccess.Models
{
    public class CustomTable
    {
        public int ClassId { get; set; }

        public string? ClassName { get; set; }

        public string? ClassDisplayName { get; set; }

        public string? ClassTableName { get; set; }

        public List<FieldDefinition> Fields { get; set; } = [];

        public List<AlternativeForm> AlternativeForms { get; set; } = [];
    }
}
