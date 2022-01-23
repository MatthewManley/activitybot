using Discord;
using Discord.WebSocket;
using Domain.Models;
using Domain.Repos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityBot
{
    public class Bot
    {
        private readonly ILogger<Bot> logger;
        private readonly DiscordSocketClient client;
        private readonly IActivityRepo activityRepo;
        private readonly IServerConfigRepo serverConfigRepo;
        private readonly IMemoryCache memoryCache;
        private readonly AuthOptions authOptions;
        private readonly CancellationTokenSource cancellationTokenSource;
        private Task checker;

        public Bot(ILogger<Bot> logger,
                   DiscordSocketClient client,
                   IActivityRepo activityRepo,
                   IServerConfigRepo serverConfigRepo,
                   IOptions<AuthOptions> authOptions,
                   IMemoryCache memoryCache)
        {
            this.logger = logger;
            this.client = client;
            this.activityRepo = activityRepo;
            this.serverConfigRepo = serverConfigRepo;
            this.memoryCache = memoryCache;
            this.authOptions = authOptions.Value;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        internal async Task Run()
        {
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += Client_MessageReceived;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

            logger.LogInformation("Logging in");
            await client.LoginAsync(TokenType.Bot, authOptions.BotKey);
            logger.LogInformation("Starting");
            await client.StartAsync();
        }

        private Task Client_Ready()
        {
            checker = Task.Run(Checker);
            return Task.CompletedTask;
        }

        private async Task Checker()
        {
            logger.LogInformation("Starting checker");
            var token = cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                await Task.Delay(1000 * 60 * 5, token);
                if (token.IsCancellationRequested)
                    return;
                var allActivities = await activityRepo.GetAll();
                var groups = allActivities.GroupBy(x => x.Server);
                foreach (var group in groups)
                {
                    var serverConfig = await CachedServerConfig(group.Key);
                    var server = client.GetGuild(group.Key);
                    var serverRole = server.GetRole(serverConfig.Role);
                    if (serverRole is null)
                        continue;
                    var duration = TimeSpan.FromMinutes(serverConfig.Duration);
                    var cutoff = now.Subtract(duration);
                    foreach (var user in group)
                    {
                        if (user.LastActivity >= cutoff)
                            continue;
                        try
                        {
                            logger.LogInformation("Guild {guild}, User: {user} no longer active", new { guild = group.Key, user.User });
                            await activityRepo.Delete(group.Key, user.User);
                            await client.Rest.RemoveRoleAsync(group.Key, user.User, serverConfig.Role);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

            }
            logger.LogInformation("Checker exited");
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState previous, SocketVoiceState current)
        {
            logger.LogInformation("User voice state changed");
            if (current.VoiceChannel is null ||
                user is not SocketGuildUser guildUser)
                return;
            await SetUserActive(guildUser);
        }

        private async Task Client_MessageReceived(SocketMessage rawMessage)
        {
            if (rawMessage.Author.IsBot || rawMessage.Author.IsWebhook)
                return;
            if (rawMessage.Author is not SocketGuildUser guildUser)
                return;
            await SetUserActive(guildUser);
        }

        private async Task<ServerConfig> CachedServerConfig(ulong guildId)
        {
            return await memoryCache.GetOrCreateAsync(guildId, async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return await serverConfigRepo.Get(guildId);
            });
        }

        private async Task SetUserActive(SocketGuildUser user)
        {
            var serverConfig = await CachedServerConfig(user.Guild.Id);
            if (serverConfig == null)
                return;
            if (memoryCache.TryGetValue(user.Id, out var _))
                return;
            memoryCache.Set<object>(user.Id, null, TimeSpan.FromSeconds(60));
            await activityRepo.InsertOrUpdate(user.Guild.Id, user.Id, DateTime.UtcNow);
            var role = user.Guild.GetRole(serverConfig.Role);
            if (role is not null && !user.Roles.Any(x => x.Id == serverConfig.Role))
            {
                await user.AddRoleAsync(role);
            }
        }

        internal async Task Stop()
        {
            cancellationTokenSource.Cancel();
            await client.StopAsync();
            await client.LogoutAsync();
        }

        private Task Log(LogMessage msg)
        {
            logger.Log(Convert(msg.Severity), msg.Exception, msg.Message, msg.Source);
            return Task.CompletedTask;
        }

        private static LogLevel Convert(LogSeverity logSeverity)
        {
            return logSeverity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Trace,
                _ => throw new NotImplementedException(),
            };
        }

    }
}
