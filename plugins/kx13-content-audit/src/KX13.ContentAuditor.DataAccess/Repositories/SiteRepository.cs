using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class SiteRepository : ISiteRepository
    {
        private readonly DbReader dbReader;

        public SiteRepository(DbReader dbReader) => this.dbReader = dbReader;

        private const string GetSitesBaseSql = """
            SELECT
                s.SiteID, s.SiteDisplayName, s.SiteName, s.SiteDomainName,
                COALESCE(s.SiteDefaultVisitorCulture, site_sk.KeyValue, global_sk.KeyValue) AS EffectiveCulture
            FROM CMS_Site AS s
            LEFT JOIN CMS_SettingsKey AS site_sk
                ON site_sk.SiteID = s.SiteID AND site_sk.KeyName = 'CMSDefaultCultureCode'
            LEFT JOIN CMS_SettingsKey AS global_sk
                ON global_sk.SiteID IS NULL AND global_sk.KeyName = 'CMSDefaultCultureCode'
            """;

        private const string GetSiteCulturesSql = """
            SELECT c.CultureCode
            FROM CMS_SiteCulture sc
            INNER JOIN CMS_Culture c ON sc.CultureID = c.CultureID
            WHERE sc.SiteID = @SiteID
            """;

        public async Task<List<Site>> GetSitesAsync(AuditFilterOptions? filter = null)
        {
            string sql = GetSitesBaseSql;
            var parameters = new List<SqlParameter>();

            if (filter?.HasSiteFilter == true)
            {
                sql += "\nWHERE s.SiteName = @SiteName";
                parameters.Add(new SqlParameter("@SiteName", filter.SiteName));
            }

            var results = await dbReader.QueryAsync(sql, parameters.ToArray());

            return results.Select(row => new Site
            {
                SiteId = Convert.ToInt32(row["SiteID"]),
                SiteDisplayName = row["SiteDisplayName"] as string,
                SiteName = row["SiteName"] as string,
                SiteDomainName = row["SiteDomainName"] as string,
                SiteDefaultCultureCode = row["EffectiveCulture"] as string
            }).ToList();
        }

        public async Task<List<string>> GetSiteCulturesAsync(int siteId)
        {
            var results = await dbReader.QueryAsync(GetSiteCulturesSql,
                new SqlParameter("@SiteID", siteId));

            return results
                .Select(row => (string)row["CultureCode"]!)
                .ToList();
        }
    }
}
