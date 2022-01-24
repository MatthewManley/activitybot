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
            return await dbConnection.QueryFirstOrDefaultAsync<ServerConfig>(cmdText, parameters);
        }

        public async Task SetRole(ulong serverId, ulong roleId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "INSERT INTO serverconfig (server, role) VALUES (@serverId, @roleId) ON DUPLICATE KEY UPDATE role=@roleId";
            var parameters = new
            {
                serverId,
                roleId,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }

        public async Task SetInactiveTime(ulong serverId, long time)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "INSERT INTO serverconfig (server, duration) VALUES (@serverId, @time) ON DUPLICATE KEY UPDATE duration=@time";
            var parameters = new
            {
                serverId,
                time,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }
    }
}
