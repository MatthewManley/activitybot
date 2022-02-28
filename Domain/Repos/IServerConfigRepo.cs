using Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repos
{
    public interface IServerConfigRepo
    {
        Task<ServerConfig> Get(ulong serverId);
        Task<IEnumerable<ServerConfig>> GetAll();
        Task SetRole(ulong serverId, ulong? roleId);
        Task SetInactiveTime(ulong serverId, long time);
    }
}
