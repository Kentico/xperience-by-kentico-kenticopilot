namespace KX13.ContentAuditor.DataAccess.Models;

/// <summary>
/// Optional filters to scope the content audit output.
/// </summary>
public class AuditFilterOptions
{
    /// <summary>
    /// Optional site code name (exact match against CMS_Site.SiteName).
    /// </summary>
    public string? SiteName { get; set; }

    /// <summary>
    /// Optional class name pattern. Supports trailing wildcard (*) translated to SQL LIKE %,
    /// and multiple comma-separated patterns (e.g. "DancingGoat.*,CMS.MenuItem").
    /// </summary>
    public string? ClassNamePattern { get; set; }

    /// <summary>
    /// Optional page path prefix. Filters content tree nodes whose NodeAliasPath starts with this value.
    /// </summary>
    public string? PagePathPrefix { get; set; }

    public bool HasSiteFilter => !string.IsNullOrWhiteSpace(SiteName);
    public bool HasClassNameFilter => !string.IsNullOrWhiteSpace(ClassNamePattern);
    public bool HasPagePathFilter => !string.IsNullOrWhiteSpace(PagePathPrefix);
    public bool HasAnyFilter => HasSiteFilter || HasClassNameFilter || HasPagePathFilter;
}
