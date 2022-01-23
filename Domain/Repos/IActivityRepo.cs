using Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repos
{
    public interface IActivityRepo
    {
        Task<ActivityEntry> Get(ulong serverId, ulong userId);
        Task InsertOrUpdate(ulong serverId, ulong userId, DateTime lastActivity);
        Task<IEnumerable<ActivityEntry>> GetAll();
        Task Delete(ulong serverId, ulong userId);
    }
}
