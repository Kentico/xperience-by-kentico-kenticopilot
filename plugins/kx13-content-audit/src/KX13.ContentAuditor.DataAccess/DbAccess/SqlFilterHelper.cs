using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.DbAccess;

/// <summary>
/// Translates user-facing class name patterns (with * wildcards) into parameterized SQL clauses.
/// </summary>
internal static class SqlFilterHelper
{
    /// <summary>
    /// Builds a SQL clause matching ClassName against one or more comma-separated patterns.
    /// Patterns with * are translated to LIKE with %, others use exact =.
    /// Returns a parenthesized OR clause, e.g. "(c.ClassName LIKE @ClassPattern0 OR c.ClassName = @ClassPattern1)".
    /// </summary>
    public static string BuildClassNameClauses(string tableAlias, string pattern, List<SqlParameter> parameters)
    {
        string[] patterns = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var clauses = new List<string>(patterns.Length);

        for (int i = 0; i < patterns.Length; i++)
        {
            string p = patterns[i];
            string paramName = $"@ClassPattern{i}";

            if (p.Contains('*'))
            {
                clauses.Add($"{tableAlias}.ClassName LIKE {paramName}");
                parameters.Add(new SqlParameter(paramName, p.Replace("*", "%")));
            }
            else
            {
                clauses.Add($"{tableAlias}.ClassName = {paramName}");
                parameters.Add(new SqlParameter(paramName, p));
            }
        }

        return $"({string.Join(" OR ", clauses)})";
    }
}
