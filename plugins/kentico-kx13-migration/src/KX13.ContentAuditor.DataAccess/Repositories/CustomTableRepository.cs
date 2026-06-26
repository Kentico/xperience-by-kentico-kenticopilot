using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class CustomTableRepository : ICustomTableRepository
    {
        private readonly DbReader dbReader;
        private readonly ClassFormDefinitionParser formDefinitionParser;

        public CustomTableRepository(DbReader dbReader, ClassFormDefinitionParser formDefinitionParser)
        {
            this.dbReader = dbReader;
            this.formDefinitionParser = formDefinitionParser;
        }

        private const string GetAllCustomTablesSql = """
            SELECT ClassID, ClassName, ClassDisplayName, ClassTableName, ClassFormDefinition
            FROM CMS_Class
            WHERE ClassIsCustomTable = 1
            """;

        private const string GetCustomTablesForSiteSql = """
            SELECT c.ClassID, c.ClassName, c.ClassDisplayName, c.ClassTableName, c.ClassFormDefinition
            FROM CMS_Class c
            INNER JOIN CMS_ClassSite cs ON c.ClassID = cs.ClassID
            WHERE c.ClassIsCustomTable = 1 AND cs.SiteID = @SiteID
            """;

        private const string GetAlternativeFormsSql = """
            SELECT FormID, FormDisplayName, FormName, FormClassID, FormDefinition, FormLayoutType
            FROM CMS_AlternativeForm
            WHERE FormClassID = @ClassID
            """;

        public async Task<List<CustomTable>> GetAllCustomTablesAsync()
        {
            var results = await dbReader.QueryAsync(GetAllCustomTablesSql);
            var tables = new List<CustomTable>(results.Count);

            foreach (var row in results)
            {
                tables.Add(await MapCustomTableAsync(row));
            }

            return tables;
        }

        public async Task<List<CustomTable>> GetCustomTablesForSiteAsync(int siteId)
        {
            var results = await dbReader.QueryAsync(GetCustomTablesForSiteSql,
                new SqlParameter("@SiteID", siteId));

            var tables = new List<CustomTable>(results.Count);

            foreach (var row in results)
            {
                tables.Add(await MapCustomTableAsync(row));
            }

            return tables;
        }

        public async Task<List<AlternativeForm>> GetAlternativeFormsAsync(int classId)
        {
            var results = await dbReader.QueryAsync(GetAlternativeFormsSql,
                new SqlParameter("@ClassID", classId));

            return results.Select(row => new AlternativeForm
            {
                FormId = Convert.ToInt32(row["FormID"]),
                FormDisplayName = row["FormDisplayName"] as string,
                FormName = row["FormName"] as string,
                FormClassId = Convert.ToInt32(row["FormClassID"]),
                FormDefinitionDelta = row["FormDefinition"] as string,
                FormLayoutType = row["FormLayoutType"] as string
            }).ToList();
        }

        private async Task<CustomTable> MapCustomTableAsync(Dictionary<string, object?> row)
        {
            int classId = Convert.ToInt32(row["ClassID"]);
            string? classFormDefinition = row["ClassFormDefinition"] as string;
            string className = row["ClassName"] as string ?? $"ClassID {row["ClassID"]}";
            string? displayName = row["ClassDisplayName"] as string;

            return new CustomTable
            {
                ClassId = classId,
                ClassName = row["ClassName"] as string,
                ClassDisplayName = row["ClassDisplayName"] as string,
                ClassTableName = row["ClassTableName"] as string,
                Fields = formDefinitionParser.TryParseFieldDefinitions(
                    classFormDefinition,
                    "Custom table",
                    className,
                    displayName),
                AlternativeForms = await GetAlternativeFormsAsync(classId)
            };
        }
    }
}
