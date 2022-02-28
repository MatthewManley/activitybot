using Discord.WebSocket;
using Domain.Repos;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class StatsCommand : ISocketSlashCommandHandler
    {
        private readonly DiscordSocketClient discordSocketClient;
        private readonly IMemoryCache memoryCache;
        private readonly IServerConfigRepo serverConfigRepo;

        public StatsCommand(DiscordSocketClient discordSocketClient, IMemoryCache memoryCache, IServerConfigRepo serverConfigRepo)
        {
            this.discordSocketClient = discordSocketClient;
            this.memoryCache = memoryCache;
            this.serverConfigRepo = serverConfigRepo;
        }

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            StatsResponse counts;
            //if (memoryCache.TryGetValue("guildCount", out counts))
            //{
            //    await slashCommand.RespondAsync($"Total Guilds: {counts.TotalCount}\nConfigured Guilds: {counts.ConfiguredCount}");
            //    return;
            //}
            await slashCommand.DeferAsync();
            var guilds = await discordSocketClient.Rest.GetGuildsAsync();
            var guildIds = guilds.Select(x => x.Id).ToHashSet();
            var configs = await serverConfigRepo.GetAll();
            var configuredCount = configs.Where(x => x.Role is not null && guildIds.Contains(x.Server)).Count();
            counts = new StatsResponse { TotalCount = guildIds.Count(), ConfiguredCount = configuredCount };
            memoryCache.Set("guildCount", counts, TimeSpan.FromMinutes(5));
            await slashCommand.FollowupAsync($"Total Guilds: {counts.TotalCount}\nConfigured Guilds: {counts.ConfiguredCount}");
        }

        private record StatsResponse
        {
            public int TotalCount { get; set; }
            public int ConfiguredCount { get; set; }
        }
    }
}
