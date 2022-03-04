using Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repos
{
    public interface IActivityRepo
    {
        Task<ActivityEntry> Get(ulong serverId, ulong userId);
        Task<IEnumerable<ActivityEntry>> GetAllForUser(ulong userId);
        Task<IEnumerable<ActivityEntry>> GetAssignedForServer(ulong serverId);
        Task InsertOrUpdate(ulong serverId, ulong userId, DateTime lastActivity, bool removed = false);
        Task<IEnumerable<ActivityEntry>> GetAll();
        Task Delete(ulong serverId, ulong userId);
        Task SetRemovalStatus(ulong serverId, ulong userId, ActivityEntryStatus activityEntryStatus);
    }

    public static class ActivityRepoExtensions
    {
        public static async Task Delete(this IActivityRepo activityRepo, ActivityEntry activityEntry)
            => await activityRepo.Delete(activityEntry.Server, activityEntry.User);
    }
}
