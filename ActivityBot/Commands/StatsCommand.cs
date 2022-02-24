using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class StatsCommand
    {
        private readonly DiscordSocketClient discordSocketClient;

        public StatsCommand(DiscordSocketClient discordSocketClient)
        {
            this.discordSocketClient = discordSocketClient;
        }

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync($"Guilds: {discordSocketClient.Guilds.Count}");
        }
    }
}
