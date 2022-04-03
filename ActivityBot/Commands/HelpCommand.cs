using Discord.WebSocket;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class HelpCommand : ISocketSlashCommandHandler
    {

        public HelpCommand()
        {
        }

        private const string helpResponse =
            "/commands\n" +
            "For a list of commands and their descriptions\n\n" +

            "/setup\n" +
            "For a checklist of steps to follow to setup the bot\n\n" +

            "If you need additional help with the bot, you can join the support server and ask for help there: https://discord.gg/czEz6u4wxB\n";

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(helpResponse, ephemeral: true);
        }
    }
}
