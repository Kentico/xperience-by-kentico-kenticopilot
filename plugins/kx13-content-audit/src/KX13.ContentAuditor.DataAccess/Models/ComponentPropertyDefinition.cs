namespace KX13.ContentAuditor.DataAccess.Models
{
    public class ComponentPropertyDefinition
    {
        public string? PropertyName { get; set; }

        public string? PropertyTypeName { get; set; }

        public string? EditingComponentIdentifier { get; set; }

        public string? Label { get; set; }

        public int Order { get; set; }

        public string? DefaultValue { get; set; }

        public Dictionary<string, string> ComponentPropertySettings { get; set; } = [];
    }
}