using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.DbAccess
{
    public class DbReader
    {
        private readonly SqlConnectionFactory connectionFactory;
        private readonly int commandTimeoutSeconds;

        public DbReader(SqlConnectionFactory connectionFactory, int commandTimeoutSeconds)
        {
            this.connectionFactory = connectionFactory;
            this.commandTimeoutSeconds = commandTimeoutSeconds;
        }

        public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, params SqlParameter[] parameters)
        {
            var results = new List<Dictionary<string, object?>>();
            await using var conn = connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = commandTimeoutSeconds;
            cmd.Parameters.AddRange(parameters);

            await using var reader = await cmd.ExecuteReaderAsync();
            var schema = await reader.GetColumnSchemaAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var col in schema)
                {
                    if (col.ColumnOrdinal is { } ord)
                    {
                        row[col.ColumnName] = await reader.IsDBNullAsync(ord) ? null : reader.GetValue(ord);
                    }
                }
                results.Add(row);
            }
            return results;
        }
    }
}
