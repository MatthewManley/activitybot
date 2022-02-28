using Discord.WebSocket;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public interface ISocketSlashCommandHandler
    {
        Task Interact(SocketSlashCommand slashCommand);
    }
}
