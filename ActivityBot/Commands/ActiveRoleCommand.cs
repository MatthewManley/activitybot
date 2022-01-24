using Discord.WebSocket;
using Domain.Repos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class ActiveRoleCommand
    {
        private readonly IServerConfigRepo serverConfigRepo;

        public ActiveRoleCommand(IServerConfigRepo serverConfigRepo)
        {
            this.serverConfigRepo = serverConfigRepo;
        }

        public async Task Execute(SocketSlashCommand slashCommand)
        {
            if (slashCommand.User is not SocketGuildUser guildUser)
            {
                await slashCommand.RespondAsync("This command can only be run in a guild", ephemeral: true);
                return;
            }
            var action = slashCommand.Data.Options.FirstOrDefault();
            if (action is null || action.Type != Discord.ApplicationCommandOptionType.SubCommand)
            {
                throw new Exception();
            }
            switch (action.Name)
            {
                case "get":
                    await GetSubcommand(slashCommand, guildUser);
                    return;
                case "set":
                    await SetSubCommand(slashCommand, guildUser);
                    return;
                default:
                    throw new Exception();
            }
        }

        private async Task GetSubcommand(SocketSlashCommand slashCommand, SocketGuildUser socketGuildUser)
        {
            var config = await serverConfigRepo.Get(socketGuildUser.Guild.Id);
            await slashCommand.RespondAsync($"<@&{config.Role}>", ephemeral: true);
        }

        private async Task SetSubCommand(SocketSlashCommand slashCommand, SocketGuildUser socketGuildUser)
        {
            if (!socketGuildUser.GuildPermissions.Administrator)
            {
                await slashCommand.RespondAsync("You must have administrator permission to run this command", ephemeral: true);
                return;
            }

            var role = slashCommand.Data.Options.FirstOrDefault()?.Value as SocketRole;
            if (role is null)
            {
                throw new Exception();
            }

            await serverConfigRepo.SetRole(socketGuildUser.Guild.Id, role.Id);
            await slashCommand.RespondAsync($"Done! Active users will now be given the role <@&{role.Id}", ephemeral: true);
        }
    }
}
