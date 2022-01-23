using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class DbConnectionFactory
    {
        private readonly string connectionString;

        public DbConnectionFactory(IOptions<DbOptions> options)
        {
            connectionString = options.Value.ConnectionString;
        }

        public async Task<IDbConnection> CreateConnection()
        {
            var connection = new MySqlConnection(connectionString);
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            return connection;
        }
    }
}
