using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.DbAccess
{
    public class SqlConnectionFactory
    {
        private readonly string connectionString;

        public SqlConnectionFactory(string connectionString) => this.connectionString = connectionString;

        public SqlConnection CreateConnection() => new(connectionString);
    }
}
