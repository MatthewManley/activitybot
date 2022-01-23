﻿using Discord.WebSocket;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;


namespace ActivityBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                builder.SetupConfiguration();
            });
            builder.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton((services) =>
                {
                    var config = new DiscordSocketConfig()
                    {
                        GatewayIntents = Discord.GatewayIntents.GuildMessages | Discord.GatewayIntents.GuildVoiceStates
                    };
                    return new DiscordSocketClient(config);
                });
                services.AddMemoryCache();
                services.Configure<AuthOptions>(hostContext.Configuration.GetSection("Auth"));
                services.AddSingleton<Bot>();
                services.AddHostedService<Startup>();
                services.ConfigureInfrastructure(hostContext.Configuration);
            }
            );
            var host = builder.Build();
            await host.RunAsync();
        }
    }

}

