namespace KX13.ContentAuditor.DataAccess.Models
{
    public class WidgetVariant
    {
        public Guid Identifier { get; set; }

        public string? Name { get; set; }

        public Dictionary<string, object?> Properties { get; set; } = [];

        public Dictionary<string, object?> ConditionTypeParameters { get; set; } = [];
    }
}