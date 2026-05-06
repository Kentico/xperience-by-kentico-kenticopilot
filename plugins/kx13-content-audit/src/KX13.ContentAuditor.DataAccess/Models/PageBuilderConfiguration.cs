namespace KX13.ContentAuditor.DataAccess.Models
{
    public class PageBuilderConfiguration
    {
        public PageTemplateConfig? Template { get; set; }

        public List<EditableAreaConfig> EditableAreas { get; set; } = [];
    }
}