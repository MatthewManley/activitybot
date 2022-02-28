using Discord.WebSocket;
using Domain.Repos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class OptCommand : ISocketSlashCommandHandler
    {
        private readonly IOptRepo optRepo;
        private readonly DiscordSocketClient discordSocketClient;
        private readonly IActivityRepo activityRepo;
        private readonly IServerConfigRepo serverConfigRepo;
        private readonly ILogger<OptCommand> logger;

        public OptCommand(IOptRepo optRepo,
                          DiscordSocketClient discordSocketClient,
                          IActivityRepo activityRepo,
                          IServerConfigRepo serverConfigRepo,
                          ILogger<OptCommand> logger)
        {
            this.optRepo = optRepo;
            this.discordSocketClient = discordSocketClient;
            this.activityRepo = activityRepo;
            this.serverConfigRepo = serverConfigRepo;
            this.logger = logger;
        }

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            var action = slashCommand.Data.Options.FirstOrDefault();
            if (action is null || action.Type != Discord.ApplicationCommandOptionType.SubCommand)
            {
                throw new Exception();
            }
            switch (action.Name)
            {
                case "in":
                    await InSubCommand(slashCommand);
                    return;
                case "out":
                    await OutSubCommand(slashCommand);
                    return;
                case "status":
                    await StatusSubCommand(slashCommand);
                    return;
                default:
                    throw new Exception();
            }
        }

        private const string OptedIn =  "You are opted in to activity tracking. You will be given active roles on servers with the bot.";
        private const string OptedOut = "You are opted out from activity tracking. You will not be given active roles on servers with the bot.";

        private async Task InSubCommand(SocketSlashCommand slashCommand)
        {
            await optRepo.Remove(slashCommand.User.Id);
            await slashCommand.RespondAsync(OptedIn, ephemeral: true);
        }

        private async Task OutSubCommand(SocketSlashCommand slashCommand)
        {
            await slashCommand.DeferAsync(ephemeral: true);
            await optRepo.Add(slashCommand.User.Id);
            var activities = await activityRepo.GetAllForUser(slashCommand.User.Id);
            foreach (var activity in activities)
            {
                if (!activity.Removed)
                {
                    var serverConfig = await serverConfigRepo.Get(activity.Server);
                    try
                    {
                        await discordSocketClient.Rest.RemoveRoleAsync(activity.Server, slashCommand.User.Id, serverConfig.Role.Value);
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        logger.LogError(ex, "Error in OptOutCommand");
                    }
                }
                await activityRepo.Delete(activity);
            }
            await slashCommand.FollowupAsync(text: OptedOut, ephemeral: true);
        }

        private async Task StatusSubCommand(SocketSlashCommand slashCommand)
        {
            var status = await optRepo.Get(slashCommand.User.Id);
            var message = status ? OptedOut : OptedIn;
            await slashCommand.RespondAsync(message, ephemeral: true);
        }
    }    
}
