namespace KX13.ContentAuditor.DataAccess.Models
{
    public class AlternativeForm
    {
        public int FormId { get; set; }

        public string? FormDisplayName { get; set; }

        public string? FormName { get; set; }

        public int FormClassId { get; set; }

        public string? FormDefinitionDelta { get; set; }

        public string? FormLayoutType { get; set; }
    }
}
