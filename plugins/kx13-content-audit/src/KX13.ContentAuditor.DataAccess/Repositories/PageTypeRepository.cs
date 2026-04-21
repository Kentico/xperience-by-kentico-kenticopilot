using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class PageTypeRepository : IPageTypeRepository
    {
        private readonly DbReader dbReader;
        private readonly ClassFormDefinitionParser formDefinitionParser;

        public PageTypeRepository(DbReader dbReader, ClassFormDefinitionParser formDefinitionParser)
        {
            this.dbReader = dbReader;
            this.formDefinitionParser = formDefinitionParser;
        }

        private const string GetAllPageTypesBaseSql = """
            SELECT c.ClassID, c.ClassName, c.ClassDisplayName, c.ClassTableName,
                   c.ClassFormDefinition, c.ClassURLPattern,
                   c.ClassUsesPageBuilder, c.ClassHasURL, c.ClassHasMetadata,
                   c.ClassIsNavigationItem, c.ClassNodeNameSource,
                   parent.ClassName AS InheritsFromClassName
            FROM CMS_Class c
            LEFT JOIN CMS_Class parent ON c.ClassInheritsFromClassID = parent.ClassID
            WHERE c.ClassIsDocumentType = 1
            """;

        private const string GetPageTypesForSiteSql = """
            SELECT c.ClassID, c.ClassName, c.ClassDisplayName, c.ClassTableName,
                   c.ClassFormDefinition, c.ClassURLPattern,
                   c.ClassUsesPageBuilder, c.ClassHasURL, c.ClassHasMetadata,
                   c.ClassIsNavigationItem, c.ClassNodeNameSource,
                   parent.ClassName AS InheritsFromClassName
            FROM CMS_Class c
            LEFT JOIN CMS_Class parent ON c.ClassInheritsFromClassID = parent.ClassID
            INNER JOIN CMS_ClassSite cs ON c.ClassID = cs.ClassID
            WHERE c.ClassIsDocumentType = 1 AND cs.SiteID = @SiteID
            """;

        public async Task<List<PageType>> GetAllPageTypesAsync(AuditFilterOptions? filter = null)
        {
            string sql = GetAllPageTypesBaseSql;
            var parameters = new List<SqlParameter>();

            if (filter?.HasClassNameFilter == true)
            {
                sql += $"\nAND {SqlFilterHelper.BuildClassNameClauses("c", filter.ClassNamePattern!, parameters)}";
            }

            var results = await dbReader.QueryAsync(sql, parameters.ToArray());
            return results.Select(MapPageType).ToList();
        }

        public async Task<List<PageType>> GetPageTypesForSiteAsync(int siteId)
        {
            var results = await dbReader.QueryAsync(GetPageTypesForSiteSql,
                new SqlParameter("@SiteID", siteId));

            return results.Select(MapPageType).ToList();
        }

        private PageType MapPageType(Dictionary<string, object?> row)
        {
            string? classFormDefinition = row["ClassFormDefinition"] as string;
            string className = row["ClassName"] as string ?? $"ClassID {row["ClassID"]}";
            string? displayName = row["ClassDisplayName"] as string;
            var fields = formDefinitionParser.TryParseFieldDefinitions(
                classFormDefinition,
                "Page type",
                className,
                displayName);

            return new PageType
            {
                ClassId = Convert.ToInt32(row["ClassID"]),
                ClassName = row["ClassName"] as string,
                ClassDisplayName = row["ClassDisplayName"] as string,
                ClassTableName = row["ClassTableName"] as string,
                HasCustomFields = row["ClassTableName"] is not null,
                PageBuilderEnabled = Convert.ToBoolean(row["ClassUsesPageBuilder"] ?? false),
                UrlEnabled = Convert.ToBoolean(row["ClassHasURL"] ?? false),
                MetadataEnabled = Convert.ToBoolean(row["ClassHasMetadata"] ?? false),
                NavigationItemEnabled = Convert.ToBoolean(row["ClassIsNavigationItem"] ?? false),
                UrlPattern = row["ClassURLPattern"] as string,
                InheritsFromClassName = row["InheritsFromClassName"] as string,
                PageNameSourceField = row["ClassNodeNameSource"] as string,
                Fields = fields
            };
        }
    }
}
