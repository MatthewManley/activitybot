using Discord.WebSocket;
using Domain.Repos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class ActiveDurationCommand
    {
        private readonly IServerConfigRepo serverConfigRepo;

        public ActiveDurationCommand(IServerConfigRepo serverConfigRepo)
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
                    await SetSubCommand(slashCommand, guildUser, action);
                    return;
                default:
                    throw new Exception();
            }
        }

        private async Task GetSubcommand(SocketSlashCommand slashCommand, SocketGuildUser socketGuildUser)
        {
            var config = await serverConfigRepo.Get(socketGuildUser.Guild.Id);
            await slashCommand.RespondAsync($"Users  will be marked as inactive after {config.Duration} hours of inactivity", ephemeral: true);
        }

        private async Task SetSubCommand(SocketSlashCommand slashCommand, SocketGuildUser socketGuildUser, SocketSlashCommandDataOption dataOption)
        {
            if (!socketGuildUser.GuildPermissions.Administrator)
            {
                await slashCommand.RespondAsync("You must have administrator permission to run this command", ephemeral: true);
                return;
            }

            var hours = dataOption.Options.FirstOrDefault()?.Value as long?;
            if (hours is null)
            {
                throw new Exception();
            }

            await serverConfigRepo.SetInactiveTime(socketGuildUser.Guild.Id, hours.Value);
            await slashCommand.RespondAsync($"Done! Users will be marked as inactive after {hours} hours of inactivity", ephemeral: true);
        }
    }
}
