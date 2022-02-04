using ActivityBot.Commands;
using Discord;
using Discord.WebSocket;
using Domain.Models;
using Domain.Repos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider serviceProvider;
        private readonly AuthOptions authOptions;
        private readonly CancellationTokenSource cancellationTokenSource;
        private Timer timer;

        public Bot(ILogger<Bot> logger,
                   DiscordSocketClient client,
                   IActivityRepo activityRepo,
                   IServerConfigRepo serverConfigRepo,
                   IOptions<AuthOptions> authOptions,
                   IMemoryCache memoryCache,
                   IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.client = client;
            this.activityRepo = activityRepo;
            this.serverConfigRepo = serverConfigRepo;
            this.memoryCache = memoryCache;
            this.serviceProvider = serviceProvider;
            this.authOptions = authOptions.Value;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        internal async Task Run()
        {
            client.Log += Log;
            client.MessageReceived += Client_MessageReceived;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
            client.InteractionCreated += Client_InteractionCreated;
            client.JoinedGuild += Client_JoinedGuild;

            logger.LogInformation("Logging in");
            await client.LoginAsync(TokenType.Bot, authOptions.BotKey);
            logger.LogInformation("Starting");
            await client.StartAsync();
            timer = new Timer(async (e) => await Checker(e), new { }, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            // TODO: replace with banned server list
            if (arg.Id == 819899570728337418)
            {
                await arg.LeaveAsync();
            }
            var channel = await client.GetChannelAsync(936310272967204905);
            if (channel is not SocketTextChannel textChannel)
            {
                logger.LogError("Invalid announce channel");
                return;
            }
            await textChannel.SendMessageAsync(
                "Joined a new server!\n" +
                $"Id: {arg.Id}\n" +
                $"Name: {arg.Name}\n" +
                $"Member Count: {arg.MemberCount}\n" +
                $"Icon: {arg.IconUrl}\n" +
                $"Owner: {arg.Owner.Username}#{arg.Owner.Discriminator}\n" +
                $"Owner Id: {arg.Owner.Id}"
               );
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            var commandHandler = serviceProvider.GetRequiredService<CommandHandler>();
            await commandHandler.Interact(arg);
        }

        private async Task Checker(object state)
        {
            var now = DateTime.UtcNow;
            logger.LogInformation("Running checker");
            var allActivities = await activityRepo.GetAll();
            var groups = allActivities.GroupBy(x => x.Server);
            foreach (var group in groups)
            {
                var serverConfig = await CachedServerConfig(group.Key);
                if (serverConfig is null || serverConfig.Role is null)
                    continue;
                var server = client.GetGuild(group.Key);
                if (server is null) // bot is no longer in the server, perhaps we should remove the configuration and all activity?
                    continue;
                var serverRole = server.GetRole(serverConfig.Role.Value);
                if (serverRole is null)
                    continue;
                var duration = TimeSpan.FromHours(serverConfig.Duration);
                var cutoff = now.Subtract(duration);
                foreach (var user in group)
                {
                    if (user.LastActivity >= cutoff)
                        continue;
                    try
                    {
                        logger.LogInformation($"Guild {group.Key}, User: {user.User} no longer active");
                        await activityRepo.SetRemoved(group.Key, user.User, true);
                        await client.Rest.RemoveRoleAsync(group.Key, user.User, serverConfig.Role.Value);
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        logger.LogError(ex, "HttpException while removing role");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while removing role!");
                    }
                }
            }
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState previous, SocketVoiceState current)
        {
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
            if (serverConfig is null || serverConfig.Role is null)
                return;

            // Stop excessive updating of user activity
            if (memoryCache.TryGetValue(user.Id, out var _))
                return;
            memoryCache.Set<object>(user.Id, null, TimeSpan.FromSeconds(60));

            await activityRepo.InsertOrUpdate(user.Guild.Id, user.Id, DateTime.UtcNow);
            var role = user.Guild.GetRole(serverConfig.Role.Value);
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
