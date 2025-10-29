using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace flibuget.Core.Configuration;

/// <summary>
/// Extension methods for configuring Perplexity API services
/// </summary>
public static class PerplexityServiceExtensions
{
    private const string PLACEHOLDER_API_KEY = "your-perplexity-api-key-here";
    private const string ENVIRONMENT_VARIABLE_NAME = "FLIBUGET_AI_APIKEY";
    private const string CONFIGURATION_KEY = "Perplexity:ApiKey";

    /// <summary>
    /// Adds Perplexity API service (AudiobookService) to the service collection
    /// Logic: If config has placeholder value, use environment variable. Otherwise, use config value.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration containing Perplexity:ApiKey</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPerplexityService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Get API key from configuration or environment
        var apiKey = GetApiKeyOrEnv(configuration[CONFIGURATION_KEY]);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        services.AddScoped(sp =>
        {
            var webService = sp.GetRequiredService<IWebService>();
            var logger = sp.GetRequiredService<ILogger<AudiobookService>>();

            logger.LogInformation("Perplexity API service configured with API key");

            return new AudiobookService(webService, logger, apiKey);
        });

        return services;
    }

    private static string GetApiKeyOrEnv(string? configApikey)
    {
        //"your-perplexity-api-key-here"
        if(!string.IsNullOrEmpty(configApikey) && configApikey != PLACEHOLDER_API_KEY)
        {
            return configApikey;
        }

        return Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE_NAME) ??
               throw new ArgumentException("API key cannot be null or empty");
    }
}
