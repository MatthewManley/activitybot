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
            serviceCollection.AddTransient<IOptRepo, OptRepo>();
            return serviceCollection;
        }

        public static IConfigurationBuilder SetupConfiguration(this IConfigurationBuilder configurationBuilder, string environment = "Development")
        {
            return configurationBuilder.AddSystemsManager($"/ActivityBot/{environment}/");
        }
    }
}
