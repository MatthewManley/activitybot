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
            return await dbConnection.QueryFirstOrDefaultAsync<ActivityEntry>(cmdText, parameters);
        }

        public async Task InsertOrUpdate(ulong serverId, ulong userId, DateTime lastActivity, bool removed = false)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "INSERT INTO activity (server, user, lastactivity, removed) VALUES (@server, @user, @lastActivity, @removed) ON DUPLICATE KEY UPDATE lastactivity=@lastActivity, removed=@removed;";
            var parameters = new
            {
                server = serverId,
                user = userId,
                lastactivity = lastActivity,
                removed = removed,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
        }

        public async Task<IEnumerable<ActivityEntry>> GetAll()
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM activity WHERE removed=FALSE;";
            return await dbConnection.QueryAsync<ActivityEntry>(cmdText);
        }

        public async Task SetRemovalStatus(ulong serverId, ulong userId, ActivityEntryStatus activityEntryStatus)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "UPDATE activity SET removed=@removed WHERE server = @server AND user = @user;";
            var parameters = new
            {
                server = serverId,
                user = userId,
                removed = activityEntryStatus,
            };
            await dbConnection.ExecuteAsync(cmdText, parameters);
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

        public async Task<IEnumerable<ActivityEntry>> GetAllForUser(ulong userId)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM activity WHERE user = @user;";
            var parameters = new
            {
                user = userId
            };
            return await dbConnection.QueryAsync<ActivityEntry>(cmdText, parameters);
        }

        public async Task<IEnumerable<ActivityEntry>> GetAllForServerWithStatus(ulong serverId, ActivityEntryStatus activityEntryStatus)
        {
            using var dbConnection = await dbConnectionFactory.CreateConnection();
            var cmdText = "SELECT * FROM activity WHERE server = @server AND removed = @status;";
            var parameters = new
            {
                server = serverId,
                status = activityEntryStatus,
            };
            return await dbConnection.QueryAsync<ActivityEntry>(cmdText, parameters);
        }
    }
}
