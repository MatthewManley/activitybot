using Discord.WebSocket;
using Domain.Repos;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class HelpCommand : ISocketSlashCommandHandler
    {
        private readonly IServerConfigRepo serverConfigRepo;
        private readonly DiscordSocketClient discordSocketClient;

        public HelpCommand(IServerConfigRepo serverConfigRepo, DiscordSocketClient discordSocketClient)
        {
            this.serverConfigRepo = serverConfigRepo;
            this.discordSocketClient = discordSocketClient;
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

            "/help\n" +
            "Display this message\n\n" +

            "**Additional Help:**\n" +
            "If you need help with the bot, you can join the support server and ask for help there: https://discord.gg/czEz6u4wxB\n" +
            "\n\n**Setup:**\n";

        private const string checkMark = "✅";
        private const string xMark = "❌";

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            var guildUser = slashCommand.User as SocketGuildUser;
            if (guildUser is null)
            {
                await slashCommand.RespondAsync(helpResponse);
                return;
            }

            var serverConfig = await serverConfigRepo.Get(guildUser.Guild.Id);
            var activeRole = guildUser.Guild.Roles.FirstOrDefault(x => x.Id == serverConfig?.Role);
            var guild = discordSocketClient.GetGuild(guildUser.Guild.Id);
            var clientGuildUser = guild?.GetUser(discordSocketClient.CurrentUser.Id);
            var highestRole = clientGuildUser?.Roles.OrderByDescending(x => x.Position).FirstOrDefault();
            StringBuilder message = new StringBuilder(helpResponse);

            message.Append(checkMark);
            message.Append("Active duration is configured to remove the active role ");
            message.Append(serverConfig?.Duration ?? 24);
            message.Append(" hours afer a users last activity\n");

            // Active Role Configured
            if (serverConfig?.Role is not null && activeRole is not null)
            {
                message.Append(checkMark);
                message.Append(" An active role is configured, ");
                message.Append(activeRole.Mention);
                message.Append(" will be assigned to active users\n");
            }
            else
            {
                message.Append(xMark);
                message.Append(" An active role is not configured, configure one with the command /activerole set @Role\n");
            }

            // Manage Roles Permission
            if ((clientGuildUser?.GuildPermissions.ManageRoles).GetValueOrDefault(false))
            {
                message.Append(checkMark);
                message.Append(" Active Role Bot has the \"Manage Roles\" permission\n");
            }
            else
            {
                message.Append(xMark);
                message.Append(" Active Role Bot does not have the \"Manage Roles\" permission, this is required to assign the active role to users\n");
            }

            // Higher role than active role
            if (highestRole is not null && activeRole is not null && highestRole.Position > activeRole.Position)
            {
                message.Append(checkMark);
                message.Append("Active Role Bot has a higher role than the configured active role");
            }
            else
            {
                message.Append(xMark);
                message.Append(" Active Role Bot does not have a higher role than the configured active role, this is required for the bot to assign the active role to users");
            }

            await slashCommand.RespondAsync(message.ToString(), ephemeral: true, allowedMentions: Discord.AllowedMentions.None);
        }
    }
}
