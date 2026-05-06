namespace KX13.ContentAuditor.DataAccess.Models
{
    public class KX13ProjectContent
    {
        public List<Site> Sites { get; set; } = [];

        public List<PageType> AllPageTypes { get; set; } = [];

        public List<CustomTable> AllCustomTables { get; set; } = [];

        public List<CustomModule> AllCustomModules { get; set; } = [];
        public List<Form> AllForms { get; set; } = [];

        public List<PageBuilderComponentDefinition> PageBuilderComponentCatalogue { get; set; } = [];

        public List<PageContentReferenceEntry> ContentReferenceGraph { get; set; } = [];

        public List<RelationshipName> RelationshipNames { get; set; } = [];

        public List<Relationship> Relationships { get; set; } = [];
        public List<AuditFailure> Failures { get; set; } = [];
    }
}
