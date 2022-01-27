using Discord.WebSocket;
using Domain.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class HelpCommand
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

            "/activeduration set [number]\n" +
            "Set the number of hours until the active role is removed from a user\n" +
            "Requires Administrator permission on the server to run this command\n\n" +

            "/activeduration get\n" +
            "Get the number of hours until the active role is removed from a user\n\n" +

            "/help\n" +
            "Display this message\n\n" +

            "**Additional Help:**\n" +
            "If you need help with the bot, you can join the support server and ask for help there: https://discord.gg/czEz6u4wxB";

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            var guildUser = slashCommand.User as SocketGuildUser;
            if (guildUser is null)
            {
                await slashCommand.RespondAsync(helpResponse);
                return;
            }

            var serverConfig = await serverConfigRepo.Get(guildUser.Guild.Id);
            var problemFound = false;

            var problemMessage = "\n\n\n**Potential Problems:**";
            var activeRole = guildUser.Guild.Roles.FirstOrDefault(x => x.Id == serverConfig?.Role);
            var guild = discordSocketClient.GetGuild(guildUser.Guild.Id);
            var clientGuildUser = guild?.GetUser(discordSocketClient.CurrentUser.Id);
            var highestRole = clientGuildUser?.Roles.OrderByDescending(x => x.Position).FirstOrDefault();

            if (serverConfig?.Role is null || activeRole is null)
            {
                problemMessage += "\n- An active role is not configured, configure one with the command /activerole set @Role";
                problemFound = true;
            }
            if (!(clientGuildUser?.GuildPermissions.ManageRoles).GetValueOrDefault(false))
            {
                problemMessage += "\n- The bot does not have the \"Manage Roles\" permission, this is required to assign the active role to users";
                problemFound = true;
            }
            if (highestRole is not null && activeRole is not null && highestRole.Position <= activeRole.Position)
            {
                problemMessage += "\n- The bot does not have a higher role than the configured active role, this is required to assign the active role to users";
                problemFound = true;
            }

            var message = helpResponse;
            if (problemFound)
                message += problemMessage;

            await slashCommand.RespondAsync(message, ephemeral: true, allowedMentions: Discord.AllowedMentions.None);
        }
    }
}
