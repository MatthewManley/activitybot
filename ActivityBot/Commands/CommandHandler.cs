using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ActivityBot.Commands
{
    public class CommandHandler
    {
        private readonly ILogger<CommandHandler> logger;
        private readonly ActiveRoleCommand activeRoleCommand;
        private readonly ActiveDurationCommand activeDurationCommand;
        private readonly HelpCommand helpCommand;
        private readonly StatsCommand statsCommand;

        public CommandHandler(ILogger<CommandHandler> logger,
                              ActiveRoleCommand activeRoleCommand,
                              ActiveDurationCommand activeDurationCommand,
                              HelpCommand helpCommand,
                              StatsCommand statsCommand)
        {
            this.logger = logger;
            this.activeRoleCommand = activeRoleCommand;
            this.activeDurationCommand = activeDurationCommand;
            this.helpCommand = helpCommand;
            this.statsCommand = statsCommand;
        }

        public async Task Interact(SocketInteraction socketInteraction)
        {
            try
            {
                if (socketInteraction is SocketSlashCommand slashCommand)
                {
                    switch (slashCommand.CommandName)
                    {
                        case "activerole":
                            await activeRoleCommand.Interact(slashCommand);
                            return;
                        case "activeduration":
                            await activeDurationCommand.Interact(slashCommand);
                            return;
                        case "help":
                            await helpCommand.Interact(slashCommand);
                            return;
                        case "stats":
                            await statsCommand.Interact(slashCommand);
                            return;
                        default:
                            await InteractRespondProblem(socketInteraction);
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                await InteractRespondProblem(socketInteraction);
                logger.LogError(ex, "Error executing interaction");
            }
        }

        private async Task InteractRespondProblem(SocketInteraction socketInteraction)
        {
            await socketInteraction.RespondAsync("Sorry, something went wrong! You can join my support server https://discord.gg/czEz6u4wxB for assistance!", ephemeral: true);
        }

        public static IServiceCollection RegisterCommands(IServiceCollection services)
        {
            services.AddTransient<CommandHandler>();
            services.AddTransient<ActiveRoleCommand>();
            services.AddTransient<ActiveDurationCommand>();
            services.AddTransient<HelpCommand>();
            services.AddTransient<StatsCommand>();
            return services;
        }
    }
}
