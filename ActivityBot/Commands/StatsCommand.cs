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
        private readonly IMemoryCache memoryCache;

        public StatsCommand(DiscordSocketClient discordSocketClient, IMemoryCache memoryCache)
        {
            this.discordSocketClient = discordSocketClient;
            this.memoryCache = memoryCache;
        }

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            int count;
            if (memoryCache.TryGetValue("guildCount", out count))
            {
                await slashCommand.RespondAsync($"Guilds: {count}");
                return;
            }
            await slashCommand.DeferAsync();
            count = discordSocketClient.Guilds.Count;
            memoryCache.Set("guildCount", count, TimeSpan.FromMinutes(1));
            await slashCommand.FollowupAsync($"Guilds: {discordSocketClient.Guilds.Count}");
        }
    }
}
