using Dapper;
using Domain.Models;
using Domain.Repos;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class ServerConfigRepo : IServerConfigRepo
    {
        private readonly DbConnectionFactory dbConnectionFactory;

        public ServerConfigRepo(DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<ServerConfig> Get(ulong serverId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM serverconfig WHERE server = @server LIMIT 1;";
            var parameters = new
            {
                server = serverId,
            };
            return await dbConnection.QueryFirstAsync<ServerConfig>(cmdText, parameters);
        }
    }
}
