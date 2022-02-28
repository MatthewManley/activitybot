using System.Threading.Tasks;

namespace Domain.Repos
{
    public interface IOptRepo
    {
        Task Add(ulong userId);
        Task Remove(ulong userId);
        Task<bool> Get(ulong userId);
    }
}
