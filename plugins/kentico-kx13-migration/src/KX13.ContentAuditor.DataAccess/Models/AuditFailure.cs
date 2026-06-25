namespace KX13.ContentAuditor.DataAccess.Models
{
    public class AuditFailure
    {
        public string Category { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string EntityIdentifier { get; set; } = string.Empty;

        public string? Context { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
