namespace KX13.ContentAuditor.DataAccess.Models
{
    public class PageTemplateConfig
    {
        public string? Identifier { get; set; }

        public Dictionary<string, object?> Properties { get; set; } = [];

        public string? PropertiesTypeName { get; set; }
    }
}