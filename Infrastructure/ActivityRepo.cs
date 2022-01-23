using Dapper;
using Domain.Models;
using Domain.Repos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class ActivityRepo : IActivityRepo
    {
        private readonly DbConnectionFactory dbConnectionFactory;

        public ActivityRepo(DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<ActivityEntry> Get(ulong serverId, ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM activity WHERE server = @server AND user = @user LIMIT 1;";
            var parameters = new
            {
                server = serverId,
                user = userId,
            };
            return await dbConnection.QueryFirstAsync<ActivityEntry>(cmdText, parameters);
        }

        public async Task InsertOrUpdate(ulong serverId, ulong userId, DateTime lastActivity)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "INSERT INTO activity (server, user, lastactivity) VALUES (@server, @user, @lastActivity) ON DUPLICATE KEY UPDATE lastactivity=@lastActivity";
            var parameters = new
            {
                server = serverId,
                user = userId,
                lastactivity = lastActivity,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }

        public async Task<IEnumerable<ActivityEntry>> GetAll()
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM activity;";
            return await dbConnection.QueryAsync<ActivityEntry>(cmdText);
        }

        public async Task Delete(ulong serverId, ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "DELETE FROM activity WHERE server = @server AND user = @user;";
            var parameters = new
            {
                server = serverId,
                user = userId,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }
    }
}