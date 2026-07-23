namespace KX13.ContentAuditor.DataAccess.Models
{
    public class ContentReference
    {
        public string? SourcePropertyName { get; set; }

        public ContentReferenceType ReferenceType { get; set; }

        public Guid? ReferencedNodeGuid { get; set; }

        public string? ReferencedNodeAliasPath { get; set; }

        public Guid? ReferencedMediaFileGuid { get; set; }

        public string? ReferencedObjectCodeName { get; set; }

        public string? ReferencedPageTypeClassName { get; set; }
    }
}