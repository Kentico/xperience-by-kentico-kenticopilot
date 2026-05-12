namespace KX13.ContentAuditor.DataAccess.Models
{
    public class FormFieldDefinition : FieldDefinition
    {
        public string? LiveSiteFormComponentIdentifier { get; set; }

        public string? ValidationRule { get; set; }

        public string? ValidationErrorMessage { get; set; }

        public string? VisibilityCondition { get; set; }

        public string? ExplanationText { get; set; }

        public string? Tooltip { get; set; }
    }
}