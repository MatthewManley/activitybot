using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ActivityBot.Commands
{
    public class CommandHandler
    {
        private readonly ILogger<CommandHandler> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly Dictionary<string, Type> pair = new Dictionary<string, Type>()
        {
            { "activerole", typeof(ActiveRoleCommand) },
            { "activeduration", typeof(ActiveDurationCommand) },
            { "help", typeof(HelpCommand) },
            { "stats", typeof(StatsCommand) },
            { "opt", typeof(OptCommand) },
        };  

        public CommandHandler(ILogger<CommandHandler> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        private ISocketSlashCommandHandler GetCommandHandler(string commandName)
        {
            if (pair.TryGetValue(commandName, out Type handlerType))
            {
                var result = serviceProvider.GetRequiredService(handlerType);
                return (ISocketSlashCommandHandler)result;
            }
            return null;
        }

        public async Task Interact(SocketInteraction socketInteraction)
        {
            try
            {
                if (socketInteraction is SocketSlashCommand slashCommand)
                {
                    var handler = GetCommandHandler(slashCommand.CommandName);
                    if (handler is null)
                    {
                        await InteractRespondProblem(socketInteraction);
                    }
                    else
                    {
                        await handler.Interact(slashCommand);
                    }
                }
                else
                {
                    await InteractRespondProblem(socketInteraction);
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
            services.AddTransient<OptCommand>();
            return services;
        }
    }
}
