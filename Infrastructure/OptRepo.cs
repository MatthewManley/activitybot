using Dapper;
using Domain.Repos;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class OptRepo : IOptRepo
    {
        private readonly DbConnectionFactory dbConnectionFactory;

        public OptRepo(DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task Add(ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "INSERT INTO optout (userid) VALUES (@userId);";
            var parameters = new
            {
                userId = userId
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }

        public async Task<bool> Get(ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM optout WHERE userid = @userId LIMIT 1;";
            var parameters = new
            {
                userId = userId
            };
            var result = await dbConnection.QueryAsync(cmdText, parameters);
            return result.Count() > 0;
        }

        public async Task Remove(ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "DELETE FROM optout WHERE userid = @userId;";
            var parameters = new
            {
                userId = userId
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }
    }
}
