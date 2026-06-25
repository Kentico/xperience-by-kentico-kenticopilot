using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class RelationshipRepository : IRelationshipRepository
    {
        private readonly DbReader dbReader;

        public RelationshipRepository(DbReader dbReader) => this.dbReader = dbReader;

        private const string GetRelationshipNamesSql = """
            SELECT rn.RelationshipNameID, rn.RelationshipName, rn.RelationshipDisplayName,
                   rn.RelationshipAllowedObjects
            FROM CMS_RelationshipName rn
            """;

        private const string GetRelationshipsForSiteSql = """
            SELECT r.RelationshipID, r.RelationshipNameID,
                   lt.NodeID AS LeftNodeID, lt.NodeAliasPath AS LeftNodeAliasPath,
                   lc.ClassName AS LeftClassName,
                   rt.NodeID AS RightNodeID, rt.NodeAliasPath AS RightNodeAliasPath,
                   rc.ClassName AS RightClassName,
                   rn.RelationshipName, rn.RelationshipDisplayName,
                   r.RelationshipIsAdHoc,
                   r.RelationshipOrder
            FROM CMS_Relationship r
            INNER JOIN CMS_RelationshipName rn ON r.RelationshipNameID = rn.RelationshipNameID
            INNER JOIN CMS_Tree lt ON r.LeftNodeID = lt.NodeID
            INNER JOIN CMS_Class lc ON lt.NodeClassID = lc.ClassID
            INNER JOIN CMS_Tree rt ON r.RightNodeID = rt.NodeID
            INNER JOIN CMS_Class rc ON rt.NodeClassID = rc.ClassID
            WHERE lt.NodeSiteID = @SiteID
            ORDER BY lc.ClassName, rn.RelationshipName, r.RelationshipOrder
            """;

        public async Task<List<RelationshipName>> GetRelationshipNamesAsync()
        {
            var results = await dbReader.QueryAsync(GetRelationshipNamesSql);

            return results.Select(row => new RelationshipName
            {
                RelationshipNameId = Convert.ToInt32(row["RelationshipNameID"]),
                Name = row["RelationshipName"] as string ?? string.Empty,
                DisplayName = row["RelationshipDisplayName"] as string,
                AllowedObjects = row["RelationshipAllowedObjects"] as string
            }).ToList();
        }

        public async Task<List<Relationship>> GetRelationshipsForSiteAsync(int siteId)
        {
            var results = await dbReader.QueryAsync(
                GetRelationshipsForSiteSql,
                new SqlParameter("@SiteID", siteId));

            return results.Select(row => new Relationship
            {
                RelationshipId = Convert.ToInt32(row["RelationshipID"]),
                RelationshipNameId = Convert.ToInt32(row["RelationshipNameID"]),
                RelationshipName = row["RelationshipName"] as string ?? string.Empty,
                RelationshipDisplayName = row["RelationshipDisplayName"] as string,
                IsAdHoc = Convert.ToBoolean(row["RelationshipIsAdHoc"] ?? false),
                LeftNodeId = Convert.ToInt32(row["LeftNodeID"]),
                LeftNodeAliasPath = row["LeftNodeAliasPath"] as string,
                LeftClassName = row["LeftClassName"] as string,
                RightNodeId = Convert.ToInt32(row["RightNodeID"]),
                RightNodeAliasPath = row["RightNodeAliasPath"] as string,
                RightClassName = row["RightClassName"] as string,
                Order = Convert.ToInt32(row["RelationshipOrder"] ?? 0)
            }).ToList();
        }
    }
}
