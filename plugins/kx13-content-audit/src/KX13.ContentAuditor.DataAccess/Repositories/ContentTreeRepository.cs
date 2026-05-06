using System.Text.RegularExpressions;
using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class ContentTreeRepository : IContentTreeRepository
    {
        private readonly DbReader dbReader;
        private readonly PageBuilderConfigParser pageBuilderParser;

        public ContentTreeRepository(DbReader dbReader, PageBuilderConfigParser pageBuilderParser)
        {
            this.dbReader = dbReader;
            this.pageBuilderParser = pageBuilderParser;
        }

        private const string GetContentTreeBaseSql = """
            SELECT t.NodeID, t.NodeGUID, t.NodeParentID, t.NodeAliasPath,
                   t.NodeLinkedNodeID, t.NodeLinkedNodeSiteID,
                   d.DocumentName, d.DocumentCulture, d.DocumentForeignKeyValue,
                   d.DocumentPageBuilderWidgets, d.DocumentPageTemplateConfiguration,
                   c.ClassName
            FROM CMS_Tree t
            LEFT JOIN CMS_Document d ON t.NodeID = d.DocumentNodeID
            INNER JOIN CMS_Class c ON t.NodeClassID = c.ClassID
            WHERE t.NodeSiteID = @SiteID
            AND (d.DocumentNodeID IS NOT NULL OR t.NodeLinkedNodeID IS NOT NULL)
            """;

        public async Task<List<ContentTreeNode>> GetContentTreeAsync(int siteId, AuditFilterOptions? filter = null, string? culture = null)
        {
            string sql = GetContentTreeBaseSql;
            var parameters = new List<SqlParameter> { new("@SiteID", siteId) };

            if (culture is not null)
            {
                sql += "\nAND (d.DocumentCulture = @Culture OR d.DocumentNodeID IS NULL)";
                parameters.Add(new SqlParameter("@Culture", culture));
            }

            if (filter?.HasClassNameFilter == true)
            {
                sql += $"\nAND {SqlFilterHelper.BuildClassNameClauses("c", filter.ClassNamePattern!, parameters)}";
            }

            if (filter?.HasPagePathFilter == true)
            {
                sql += "\nAND (t.NodeAliasPath = @ExactPath OR t.NodeAliasPath LIKE @PagePathPrefix)";
                string prefix = filter.PagePathPrefix!.TrimEnd('%').TrimEnd('/');
                parameters.Add(new SqlParameter("@ExactPath", prefix));
                parameters.Add(new SqlParameter("@PagePathPrefix", prefix + "/%"));
            }

            sql += "\nORDER BY t.NodeLevel, t.NodeOrder";

            var results = await dbReader.QueryAsync(sql, parameters.ToArray());

            return results.Select(row =>
            {
                string? widgetsJson = row["DocumentPageBuilderWidgets"] as string;
                string? templateJson = row["DocumentPageTemplateConfiguration"] as string;
                string path = row["NodeAliasPath"] as string ?? $"NodeID {row["NodeID"]}";
                string? className = row["ClassName"] as string;
                string context = $"SiteID {siteId}, Class {className ?? "unknown"}";

                return new ContentTreeNode
                {
                    NodeId = Convert.ToInt32(row["NodeID"]),
                    NodeGuid = (Guid)row["NodeGUID"]!,
                    NodeParentId = row["NodeParentID"] is not null ? Convert.ToInt32(row["NodeParentID"]) : null,
                    NodeAliasPath = row["NodeAliasPath"] as string,
                    NodeLinkedNodeId = row["NodeLinkedNodeID"] is not null ? Convert.ToInt32(row["NodeLinkedNodeID"]) : null,
                    NodeLinkedNodeSiteId = row["NodeLinkedNodeSiteID"] is not null ? Convert.ToInt32(row["NodeLinkedNodeSiteID"]) : null,
                    DocumentName = row["DocumentName"] as string,
                    PageTypeClassName = row["ClassName"] as string,
                    PageBuilderConfig = pageBuilderParser.TryParsePageBuilderConfiguration(widgetsJson, templateJson, path, context)
                };
            }).ToList();
        }

        public async Task<List<ContentTreeNode>> GetNodesByIdsAsync(IEnumerable<int> nodeIds)
        {
            int[] distinctNodeIds = [.. nodeIds.Distinct()];

            if (distinctNodeIds.Length == 0)
            {
                return [];
            }

            var parameters = distinctNodeIds
                .Select((nodeId, index) => new SqlParameter($"@NodeId{index}", nodeId))
                .ToArray();

            string parameterList = string.Join(", ", parameters.Select(p => p.ParameterName));

            string sql = $"""
                SELECT t.NodeID, t.NodeGUID, t.NodeParentID, t.NodeAliasPath,
                       t.NodeLinkedNodeID, t.NodeLinkedNodeSiteID,
                       c.ClassName
                FROM CMS_Tree t
                INNER JOIN CMS_Class c ON t.NodeClassID = c.ClassID
                WHERE t.NodeID IN ({parameterList})
                """;

            var results = await dbReader.QueryAsync(sql, parameters);

            return results.Select(row => new ContentTreeNode
            {
                NodeId = Convert.ToInt32(row["NodeID"]),
                NodeGuid = (Guid)row["NodeGUID"]!,
                NodeParentId = row["NodeParentID"] is not null ? Convert.ToInt32(row["NodeParentID"]) : null,
                NodeAliasPath = row["NodeAliasPath"] as string,
                NodeLinkedNodeId = row["NodeLinkedNodeID"] is not null ? Convert.ToInt32(row["NodeLinkedNodeID"]) : null,
                NodeLinkedNodeSiteId = row["NodeLinkedNodeSiteID"] is not null ? Convert.ToInt32(row["NodeLinkedNodeSiteID"]) : null,
                PageTypeClassName = row["ClassName"] as string
            }).ToList();
        }

        /// <summary>
        /// Queries coupled data for all nodes of a given page type within a site.
        /// Returns row-level field values keyed by NodeID.
        /// </summary>
        public async Task<List<(int NodeId, Dictionary<string, object?> FieldValues)>> GetCoupledDataForSiteNodesAsync(
            int siteId, string tableName, string primaryKeyColumn, string? culture = null)
        {
            ValidateTableName(tableName);
            ValidateTableName(primaryKeyColumn);

            string sql = $"""
                SELECT t.NodeID, ct.*
                FROM [{tableName}] ct
                INNER JOIN CMS_Document d ON ct.[{primaryKeyColumn}] = d.DocumentForeignKeyValue
                INNER JOIN CMS_Tree t ON d.DocumentNodeID = t.NodeID
                WHERE t.NodeSiteID = @SiteID
                """;

            var parameters = new List<SqlParameter> { new("@SiteID", siteId) };

            if (culture is not null)
            {
                sql += "\nAND d.DocumentCulture = @Culture";
                parameters.Add(new SqlParameter("@Culture", culture));
            }

            var results = await dbReader.QueryAsync(sql, [.. parameters]);
            var rows = new List<(int NodeId, Dictionary<string, object?> FieldValues)>();

            foreach (var row in results)
            {
                if (!row.TryGetValue("NodeID", out object? nodeIdObj) || nodeIdObj is null)
                {
                    continue;
                }

                int nodeId = Convert.ToInt32(nodeIdObj);

                var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in row)
                {
                    if (string.Equals(kvp.Key, "NodeID", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kvp.Key, primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    fields[kvp.Key] = kvp.Value;
                }

                rows.Add((nodeId, fields));
            }

            return rows;
        }

        private static void ValidateTableName(string name)
        {
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_\.]*$"))
            {
                throw new ArgumentException($"Invalid SQL identifier: {name}");
            }
        }
    }
}
