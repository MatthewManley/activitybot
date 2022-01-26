using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class CommandHandler
    {
        private readonly ILogger<CommandHandler> logger;
        private readonly ActiveRoleCommand activeRoleCommand;
        private readonly ActiveDurationCommand activeDurationCommand;

        public CommandHandler(ILogger<CommandHandler> logger, ActiveRoleCommand activeRoleCommand, ActiveDurationCommand activeDurationCommand)
        {
            this.logger = logger;
            this.activeRoleCommand = activeRoleCommand;
            this.activeDurationCommand = activeDurationCommand;
        }

        public async Task Execute(SocketInteraction socketInteraction)
        {
            try
            {
                if (socketInteraction is SocketSlashCommand slashCommand)
                {
                    switch (slashCommand.CommandName)
                    {
                        case "activerole":
                            await activeRoleCommand.Execute(slashCommand);
                            return;
                        case "activeduration":
                            await activeDurationCommand.Execute(slashCommand);
                            return;
                        default:
                            await RespondProblem(socketInteraction);
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                await RespondProblem(socketInteraction);
                logger.LogError(ex, "Error executing interaction");
            }
        }

        private async Task RespondProblem(SocketInteraction socketInteraction)
        {
            await socketInteraction.RespondAsync("Sorry, something went wrong! Message Matt#3809 or join my server https://discord.gg/czEz6u4wxB for assistance!", ephemeral: true);
        }

        public static IServiceCollection RegisterCommands(IServiceCollection services)
        {
            services.AddTransient<CommandHandler>();
            services.AddTransient<ActiveRoleCommand>();
            services.AddTransient<ActiveDurationCommand>();
            return services;
        }
    }
}
