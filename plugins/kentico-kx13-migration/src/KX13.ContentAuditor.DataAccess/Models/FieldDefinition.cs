namespace KX13.ContentAuditor.DataAccess.Models
{
    public class FieldDefinition
    {
        public Guid? FieldGuid { get; set; }

        public string? FieldName { get; set; }

        public string? FieldCaption { get; set; }

        public string? DataType { get; set; }

        public int? Size { get; set; }

        public int? Precision { get; set; }

        public bool IsRequired { get; set; }

        public string? DefaultValue { get; set; }

        public bool IsVisible { get; set; } = true;

        public bool IsSystemPageField { get; set; }

        public string? FormControlName { get; set; }

        public string? FormComponentIdentifier { get; set; }

        public Dictionary<string, string> FormControlSettings { get; set; } = [];

        public string? ReferenceToObjectType { get; set; }

        public string? ReferenceType { get; set; }

        public string? Category { get; set; }

        public int Order { get; set; }
    }
}