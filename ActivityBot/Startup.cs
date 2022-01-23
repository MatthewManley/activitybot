using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityBot
{
    public class Startup : BackgroundService
    {
        private readonly IServiceProvider services;

        public Startup(IServiceProvider services)
        {
            this.services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = services.CreateScope();
            var bot = scope.ServiceProvider.GetRequiredService<Bot>();
            await bot.Run();
            await Task.Delay(-1, stoppingToken);
            await bot.Stop();
        }
    }
}
