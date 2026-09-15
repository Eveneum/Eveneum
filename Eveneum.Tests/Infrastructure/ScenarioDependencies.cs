using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;
using System;

namespace Eveneum.Tests.Infrastructure
{
    static class ScenarioDependencies
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();

            services.AddScoped<NewtonsoftCosmosDbContext>();
            services.AddScoped<NewtonsoftLinuxCosmosDbContext>();
            services.AddScoped<SystemTextJsonCosmosDbContext>();
            services.AddScoped<SystemTextJsonLinuxCosmosDbContext>();

            var emulatorOs = Environment.GetEnvironmentVariable("CosmosDbEmulator.OS", EnvironmentVariableTarget.User) ?? "Windows,Linux";

            if (emulatorOs.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<CosmosDbContext>(provider => provider.GetRequiredService<NewtonsoftCosmosDbContext>());
                services.AddScoped<CosmosDbContext>(provider => provider.GetRequiredService<SystemTextJsonCosmosDbContext>());
            }

            if(emulatorOs.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<CosmosDbContext>(provider => provider.GetRequiredService<NewtonsoftLinuxCosmosDbContext>());
                services.AddScoped<CosmosDbContext>(provider => provider.GetRequiredService<SystemTextJsonLinuxCosmosDbContext>());
            }

            return services;
        }
    }
}
