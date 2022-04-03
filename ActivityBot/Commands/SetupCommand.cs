using Discord.WebSocket;
using Domain.Repos;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace ActivityBot.Commands
{
    public class SetupCommand : ISocketSlashCommandHandler
    {
        private readonly IServerConfigRepo serverConfigRepo;
        private readonly DiscordSocketClient discordSocketClient;

        public SetupCommand(IServerConfigRepo serverConfigRepo, DiscordSocketClient discordSocketClient)
        {
            this.serverConfigRepo = serverConfigRepo;
            this.discordSocketClient = discordSocketClient;
        }

        private const string checkMark = "✅";
        private const string xMark = "❌";

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            var guildUser = slashCommand.User as SocketGuildUser;
            if (guildUser is null)
            {
                await slashCommand.RespondAsync("This command can only be run in a server/");
                return;
            }

            var serverConfig = await serverConfigRepo.Get(guildUser.Guild.Id);
            var activeRole = guildUser.Guild.Roles.FirstOrDefault(x => x.Id == serverConfig?.Role);
            var guild = discordSocketClient.GetGuild(guildUser.Guild.Id);
            var clientGuildUser = guild?.GetUser(discordSocketClient.CurrentUser.Id);
            var highestRole = clientGuildUser?.Roles.OrderByDescending(x => x.Position).FirstOrDefault();

            var isActiveRoleConfigured = serverConfig?.Role is not null && activeRole is not null;
            var hasManageRolesPermission = (clientGuildUser?.GuildPermissions.ManageRoles).GetValueOrDefault(false);
            var hasHigherRoleThanActiveRole = highestRole is not null && activeRole is not null && highestRole.Position > activeRole.Position;
            
            StringBuilder message = new StringBuilder();
            
            message.Append(checkMark);
            message.Append("Active duration is configured to remove the active role ");
            message.Append(serverConfig?.Duration ?? 24);
            message.Append(" hours afer a users last activity\n");

            if (isActiveRoleConfigured)
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

            if (hasManageRolesPermission)
            {
                message.Append(checkMark);
                message.Append(" Active Role Bot has the \"Manage Roles\" permission\n");
            }
            else
            {
                message.Append(xMark);
                message.Append(" Active Role Bot does not have the \"Manage Roles\" permission, this is required to assign the active role to users\n");
            }

            if (hasHigherRoleThanActiveRole)
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
