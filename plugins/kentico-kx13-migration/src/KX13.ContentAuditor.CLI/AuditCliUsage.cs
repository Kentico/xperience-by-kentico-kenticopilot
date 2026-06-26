namespace KX13.ContentAuditor.CLI
{
    internal static class AuditCliUsage
    {
        private const string UsageText = @"KX13 Content Auditor — exports Kentico Xperience 13 content model as JSON

Usage:
    KX13.ContentAuditor.CLI [options]

Export options:
    --sites                      Export sites with cultures, content tree, and assignments
    --page-types                 Export all page types with field definitions
    --page-builder-components    Export discovered page builder components
    --custom-modules             Export custom modules with classes
    --custom-tables              Export custom tables with fields
    --forms                      Export all forms with fields and alternative forms
    --relationships              Export page-to-page relationships
    --report                     Generate a Markdown content model report

Filter options:
    --site-name <name>           Filter by site code name (e.g., DancingGoatMvc)
    --class-name <pattern>       Filter by class name pattern (e.g., DancingGoat.*)
                                                             Supports * wildcard and comma-separated patterns
    --page-path <prefix>         Filter content tree by path prefix (e.g., /Articles)

Other options:
    --output <path>              Output directory (default: audit-results/ under the project root)
    --help, -h                   Show this help message

Running without export options exports the full content model.
Multiple options can be combined.

Examples:
    KX13.ContentAuditor.CLI                                      # Full export
    KX13.ContentAuditor.CLI --site-name DancingGoatMvc           # Single site only
    KX13.ContentAuditor.CLI --class-name ""DancingGoat.*""         # Matching page types only
    KX13.ContentAuditor.CLI --page-types --page-path /Articles   # Page types filtered by path
    KX13.ContentAuditor.CLI --relationships --site-name DancingGoatMvc  # Site relationships
    KX13.ContentAuditor.CLI --forms --output ./my-output         # Forms to custom directory";

        public static void WriteTo(TextWriter writer) => writer.WriteLine(UsageText);
    }
}