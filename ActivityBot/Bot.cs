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
        private readonly IOptRepo optRepo;
        private readonly AuthOptions authOptions;
        private readonly CancellationTokenSource cancellationTokenSource;
        private Timer timer;

        public Bot(ILogger<Bot> logger,
                   DiscordSocketClient client,
                   IActivityRepo activityRepo,
                   IServerConfigRepo serverConfigRepo,
                   IOptions<AuthOptions> authOptions,
                   IMemoryCache memoryCache,
                   IServiceProvider serviceProvider,
                   IOptRepo optRepo)
        {
            this.logger = logger;
            this.client = client;
            this.activityRepo = activityRepo;
            this.serverConfigRepo = serverConfigRepo;
            this.memoryCache = memoryCache;
            this.serviceProvider = serviceProvider;
            this.optRepo = optRepo;
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
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            var commandHandler = serviceProvider.GetRequiredService<CommandHandler>();
            await commandHandler.Interact(arg);
        }

        private async Task Checker(object state)
        {
            logger.LogDebug("Running checker");
            var allActivities = await activityRepo.GetAll();
            var groups = allActivities.GroupBy(x => x.Server);
            var serverConfigs = await serverConfigRepo.GetAllWithRole();
            foreach (var serverConfig in serverConfigs)
            {
                var duration = TimeSpan.FromHours(serverConfig.Duration);
                var cutoff = DateTime.UtcNow.Subtract(duration);
                logger.LogDebug("Running checker for server {Server} with cutoff {cutoff}", serverConfig.Server, cutoff);
                var activities = await activityRepo.GetAllForServerWithStatus(serverConfig.Server, ActivityEntryStatus.Assigned);
                bool shouldBreak = false;
                foreach (var activityEntry in activities)
                {
                    if (shouldBreak)
                        break;

                    if (activityEntry.LastActivity >= cutoff)
                        continue;

                    try
                    {
                        logger.LogDebug("Removing Role {Role} from User {User} in guild {Guild}", serverConfig.Role, activityEntry.User, activityEntry.Server);
                        await client.Rest.RemoveRoleAsync(serverConfig.Server, activityEntry.User, serverConfig.Role.Value);
                        await activityRepo.SetRemovalStatus(serverConfig.Server, activityEntry.User, ActivityEntryStatus.Removed);
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        // These errors are caused by server level issues that won't be fixed without intervention, remove configured role so prevent further checking
                        switch (ex.DiscordCode)
                        {
                            // Bot is no longer in guild
                            case DiscordErrorCode.MissingPermissions:
                                logger.LogInformation(ex, "While removing role {Role} from user {User} in guild {Guild}, bot is no longer in guild", serverConfig.Role, activityEntry.User, serverConfig.Server);
                                await serverConfigRepo.SetRole(serverConfig.Server, null);
                                shouldBreak = true;
                                break;

                            // Role no longer exists
                            case DiscordErrorCode.UnknownRole:
                                logger.LogInformation(ex, "While removing role {Role} from user {User} in guild {Guild}, role no longer exists", serverConfig.Role, activityEntry.User, serverConfig.Server);
                                await serverConfigRepo.SetRole(serverConfig.Server, null);
                                shouldBreak = true;
                                break;

                            // User is no longer in guild
                            case DiscordErrorCode.UnknownMember:
                                logger.LogInformation("While removing role {Role} from user {User} in guild {Guild}, User no longer in guild", serverConfig.Role, activityEntry.User, serverConfig.Server);
                                await activityRepo.Delete(activityEntry);
                                break;

                            // The bot doesn't have permission to remove roles
                            case DiscordErrorCode.InsufficientPermissions:
                                logger.LogInformation("While removing role {Role} from user {User} in guild {Guild}, Insufficient permissions to remove role", serverConfig.Role, activityEntry.User, serverConfig.Server);
                                shouldBreak = true;
                                break;

                            default:
                                logger.LogError(ex, "Unknown HttpException while removing role {Role} from user {User} in guild {Guild}", serverConfig.Role, activityEntry.User, serverConfig.Server);
                                await activityRepo.SetRemovalStatus(serverConfig.Server, activityEntry.User, ActivityEntryStatus.RemovalError);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while removing role {Role} from user {User} in guild {Guild}", serverConfig.Role, activityEntry.User, serverConfig.Server);
                        await activityRepo.SetRemovalStatus(serverConfig.Server, activityEntry.User, ActivityEntryStatus.RemovalError);
                    }
                }
            }
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState previous, SocketVoiceState current)
        {
            if (user.IsBot || user.IsWebhook)
                return;
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
            if (user.IsBot || user.IsWebhook)
                return;
            var serverConfig = await CachedServerConfig(user.Guild.Id);
            if (serverConfig is null || serverConfig.Role is null)
                return;

            var role = user.Guild.GetRole(serverConfig.Role.Value);
            if (role is null)
                return;

            if (!user.Guild.CurrentUser.GuildPermissions.ManageRoles)
                return;

            var highestRole = user.Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).FirstOrDefault();
            if (highestRole is null || highestRole.Position < role.Position)
                return;

            var optedOut = await memoryCache.GetOrCreateAsync($"optout:{user.Id}", async (ICacheEntry cacheEntry) => {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2);
                return await optRepo.Get(user.Id);
             });
            if (optedOut)
                return;

            // Stop excessive updating of user activity
            var key = (user.Id, user.Guild.Id);
            if (memoryCache.TryGetValue(key, out var _))
                return;
            memoryCache.Set<object>(key, null, TimeSpan.FromSeconds(60));

            await activityRepo.InsertOrUpdate(user.Guild.Id, user.Id, DateTime.UtcNow);
            if (!user.Roles.Any(x => x.Id == serverConfig.Role))
            {
                logger.LogDebug("Gave role {Role} to user {User} in guild {Guild}", role.Id, user.Id, user.Guild.Id);
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
