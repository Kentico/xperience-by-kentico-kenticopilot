using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess
{
    public class AuditFailureCollector
    {
        private readonly List<AuditFailure> failures = [];

        public void Record(string category, string entityType, string entityIdentifier, string? context, Exception exception)
        {
            var failure = new AuditFailure
            {
                Category = category,
                EntityType = entityType,
                EntityIdentifier = entityIdentifier,
                Context = context,
                ErrorMessage = $"{exception.GetType().Name}: {exception.Message}"
            };

            failures.Add(failure);
        }

        public List<AuditFailure> GetFailures()
        {
            return failures.ToList();
        }
    }
}
