namespace KX13.ContentAuditor.DataAccess.Models
{
    public class PageContentReferenceEntry
    {
        public Guid SourceNodeGuid { get; set; }

        public string? SourceNodeAliasPath { get; set; }

        public string? SourcePageTypeClassName { get; set; }

        public string? WidgetTypeIdentifier { get; set; }

        public string? PropertyName { get; set; }

        public ContentReferenceType ReferenceType { get; set; }

        public Guid? TargetNodeGuid { get; set; }

        public string? TargetNodeAliasPath { get; set; }

        public Guid? TargetMediaFileGuid { get; set; }

        public string? TargetPageTypeClassName { get; set; }
    }
}