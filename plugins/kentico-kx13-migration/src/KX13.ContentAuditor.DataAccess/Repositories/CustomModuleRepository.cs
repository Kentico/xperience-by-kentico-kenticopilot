using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class CustomModuleRepository : ICustomModuleRepository
    {
        private readonly DbReader dbReader;
        private readonly ClassFormDefinitionParser formDefinitionParser;

        public CustomModuleRepository(DbReader dbReader, ClassFormDefinitionParser formDefinitionParser)
        {
            this.dbReader = dbReader;
            this.formDefinitionParser = formDefinitionParser;
        }

        private const string GetCustomModulesSql = """
            SELECT DISTINCT
                r.ResourceID,
                r.ResourceDisplayName,
                r.ResourceName,
                r.ResourceDescription,
                r.ResourceIsInDevelopment
            FROM CMS_Resource AS r
            WHERE UPPER(r.ResourceName) NOT LIKE 'CMS%'
            AND EXISTS (
                    SELECT 1
                    FROM CMS_Class AS c
                    WHERE c.ClassResourceID = r.ResourceID
                    AND c.ClassIsDocumentType = 0
                    AND c.ClassIsCustomTable = 0
                    AND c.ClassIsForm = 0
                    AND UPPER(c.ClassName) NOT LIKE 'CMS.%'
                    AND UPPER(c.ClassTableName) NOT LIKE 'CMS[_]%'
            )
            """;

        private const string GetModuleClassesSql = """
            SELECT c.ClassID,
                c.ClassName,
                c.ClassDisplayName,
                c.ClassTableName,
                c.ClassFormDefinition,
                parent.ClassName AS ParentClassName
            FROM CMS_Class AS c
            LEFT JOIN CMS_Class AS parent
                ON c.ClassInheritsFromClassID = parent.ClassID
            WHERE c.ClassResourceID = @ResourceID
            AND c.ClassIsDocumentType = 0
            AND c.ClassIsCustomTable = 0
            AND c.ClassIsForm = 0
            AND UPPER(c.ClassName) NOT LIKE 'CMS.%'
            AND UPPER(c.ClassTableName) NOT LIKE 'CMS[_]%'
            """;

        public async Task<List<CustomModule>> GetCustomModulesAsync()
        {
            var results = await dbReader.QueryAsync(GetCustomModulesSql);
            var modules = new List<CustomModule>(results.Count);

            foreach (var row in results)
            {
                int resourceId = Convert.ToInt32(row["ResourceID"]);

                modules.Add(new CustomModule
                {
                    ResourceId = resourceId,
                    ResourceDisplayName = row["ResourceDisplayName"] as string,
                    ResourceName = row["ResourceName"] as string,
                    ResourceDescription = row["ResourceDescription"] as string,
                    IsInDevelopment = Convert.ToBoolean(row["ResourceIsInDevelopment"] ?? false),
                    Classes = await GetModuleClassesAsync(resourceId)
                });
            }

            return modules;
        }

        public async Task<List<ModuleClass>> GetModuleClassesAsync(int resourceId)
        {
            var results = await dbReader.QueryAsync(GetModuleClassesSql,
                new SqlParameter("@ResourceID", resourceId));

            return results.Select(MapModuleClass).ToList();
        }

        private ModuleClass MapModuleClass(Dictionary<string, object?> row)
        {
            string? classFormDefinition = row["ClassFormDefinition"] as string;
            string className = row["ClassName"] as string ?? $"ClassID {row["ClassID"]}";
            string? displayName = row["ClassDisplayName"] as string;
            var fields = formDefinitionParser.TryParseFieldDefinitions(
                classFormDefinition,
                "Module class",
                className,
                displayName);

            var references = fields
                .Where(f => !string.IsNullOrEmpty(f.ReferenceToObjectType))
                .Select(f => new ModuleClassReference
                {
                    FieldName = f.FieldName,
                    TargetObjectType = f.ReferenceToObjectType,
                    DependencyType = f.ReferenceType
                })
                .ToList();

            return new ModuleClass
            {
                ClassId = Convert.ToInt32(row["ClassID"]),
                ClassName = row["ClassName"] as string,
                ClassDisplayName = row["ClassDisplayName"] as string,
                ClassTableName = row["ClassTableName"] as string,
                ParentClassName = row["ParentClassName"] as string,
                Fields = fields,
                References = references
            };
        }
    }
}
