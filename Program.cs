using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GithubStats
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = ConfigureServices();
            var githubService = serviceProvider.GetService<GitHubService>();
            var letterCounter = serviceProvider.GetService<LetterCounter>();
            var logger = serviceProvider.GetService<ILogger<Program>>();

            logger.LogInformation("Fetching repository contents...");

            try
            {
                await githubService.ProcessRepository(letterCounter);
                letterCounter.DisplayResults();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching repository contents");
                Environment.Exit(1);
            }
            finally
            {
                // Ensure all logs are written before exit
                if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            logger.LogInformation("Processing completed.");
        }

        private static ServiceProvider ConfigureServices()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging")); 
                    builder.AddConsole();
                }).AddSingleton<GitHubService>()
                .AddSingleton<LetterCounter>()
                .BuildServiceProvider();
        }
    }
}
