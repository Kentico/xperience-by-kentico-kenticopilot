namespace KX13.ContentAuditor.DataAccess.Models
{
    public class RelationshipName
    {
        public int RelationshipNameId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public bool IsAdHoc { get; set; }

        public string? AllowedObjects { get; set; }

        public Guid? SourceFieldGuid { get; set; }

        public string? SourcePageTypeClassName { get; set; }

        public string? SourceFieldName { get; set; }
    }
}
