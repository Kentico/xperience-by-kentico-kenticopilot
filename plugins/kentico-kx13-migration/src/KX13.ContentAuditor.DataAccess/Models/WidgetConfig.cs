namespace KX13.ContentAuditor.DataAccess.Models
{
    public class WidgetConfig
    {
        public string? TypeIdentifier { get; set; }

        public Dictionary<string, object?> Properties { get; set; } = [];

        public string? PropertiesTypeName { get; set; }

        public string? PersonalizationConditionTypeIdentifier { get; set; }

        public List<WidgetVariant> Variants { get; set; } = [];

        public List<ContentReference> ContentReferences { get; set; } = [];
    }
}