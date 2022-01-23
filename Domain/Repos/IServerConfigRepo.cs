using Domain.Models;
using System.Threading.Tasks;

namespace Domain.Repos
{
    public interface IServerConfigRepo
    {
        Task<ServerConfig> Get(ulong serverId);
    }
}