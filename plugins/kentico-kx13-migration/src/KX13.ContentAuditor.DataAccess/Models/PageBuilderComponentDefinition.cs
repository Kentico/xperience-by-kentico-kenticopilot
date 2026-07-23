namespace KX13.ContentAuditor.DataAccess.Models
{
    public class PageBuilderComponentDefinition
    {
        public string? Identifier { get; set; }

        public string? DisplayName { get; set; }

        public PageBuilderComponentKind Kind { get; set; }

        public string? PropertiesTypeFullName { get; set; }

        public string? ViewComponentTypeFullName { get; set; }

        public string? CustomViewName { get; set; }

        public string? IconClass { get; set; }

        public List<string> AllowedForPageTypes { get; set; } = [];

        public List<ComponentPropertyDefinition> PropertyDefinitions { get; set; } = [];
    }
}