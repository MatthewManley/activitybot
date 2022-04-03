using Discord.WebSocket;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class CommandsCommand : ISocketSlashCommandHandler
    {
        public CommandsCommand()
        {

        }

        private const string helpResponse =
            "**Commands:**\n" +

            "/activerole set @Role\n" +
            "Set the role that is assigned to active users\n" +
            "Requires Administrator permission on the server to run this command\n\n" +

            "/activerole get\n" +
            "Get the role that is assigned to active users\n\n" +

            "/activerole delete\n" +
            "Stop the bot from assigning and removing roles from users\n\n" +
            "Requires Administrator permission on the server to run this command\n\n" +

            "/activeduration set [number]\n" +
            "Set the number of hours until the active role is removed from a user\n" +
            "Requires Administrator permission on the server to run this command\n\n" +

            "/activeduration get\n" +
            "Get the number of hours until the active role is removed from a user\n\n" +

            "/lastactivity @User\n" +
            "Get the approximate last active time for a user\n\n" +

            "/commands\n" +
            "Displays this message\n\n" +

            "/setup\n" +
            "View a setup checklist to help you get the bot working in your server\n\n" +

            "/help\n" +
            "Get help with the bot\n\n";

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync(helpResponse, ephemeral: true);
        }
    }
}
