namespace KX13.ContentAuditor.DataAccess.Models
{
    public class EditableAreaConfig
    {
        public string? Identifier { get; set; }

        public List<SectionConfig> Sections { get; set; } = [];
    }
}