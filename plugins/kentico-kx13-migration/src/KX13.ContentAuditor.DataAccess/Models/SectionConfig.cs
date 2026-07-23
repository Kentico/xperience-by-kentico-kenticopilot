namespace KX13.ContentAuditor.DataAccess.Models
{
    public class SectionConfig
    {
        public string? TypeIdentifier { get; set; }

        public Dictionary<string, object?> Properties { get; set; } = [];

        public string? PropertiesTypeName { get; set; }

        public List<WidgetZoneConfig> Zones { get; set; } = [];
    }
}