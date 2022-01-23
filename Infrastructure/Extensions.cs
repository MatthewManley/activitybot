using Domain.Repos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class Extensions
    {
        public static IServiceCollection ConfigureInfrastructure(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddDefaultAWSOptions(configuration.GetAWSOptions());
            serviceCollection.Configure<DbOptions>(configuration.GetSection("Database"));
            serviceCollection.AddTransient<DbConnectionFactory>();
            serviceCollection.AddTransient<IActivityRepo, ActivityRepo>();
            serviceCollection.AddTransient<IServerConfigRepo, ServerConfigRepo>();
            return serviceCollection;
        }

        public static IConfigurationBuilder SetupConfiguration(this IConfigurationBuilder configurationBuilder)
        {
            return configurationBuilder.AddSystemsManager("/ActivityBot/Production/");
        }
    }
}
