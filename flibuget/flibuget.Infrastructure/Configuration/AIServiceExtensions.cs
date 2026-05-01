using flibuget.Core.InfraServices.AI;
using flibuget.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace flibuget.Infrastructure.Configuration;

public static class AIServiceExtensions
{
    private const string PLACEHOLDER_API_KEY = "your-perplexity-api-key-here";

    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var providerConfigs = new Dictionary<string, AIProviderConfig>();

        var openAIConfig = ConfigureOpenAI(configuration);
        if (openAIConfig != null)
            providerConfigs["OpenAI"] = openAIConfig;

        var perplexityConfig = ConfigurePerplexity(configuration);
        if (perplexityConfig != null)
            providerConfigs["Perplexity"] = perplexityConfig;

        if (providerConfigs.Count == 0)
            throw new InvalidOperationException(
                "No AI providers configured. Please configure at least one provider in appsettings.json");

        var defaultProvider = configuration["AI:DefaultProvider"] ?? "OpenAI";

        if (!providerConfigs.ContainsKey(defaultProvider))
            throw new InvalidOperationException($"Default provider '{defaultProvider}' is not configured");

        services.AddSingleton<IAIProviderFactory>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AIProviderFactory>>();
            return new AIProviderFactory(sp, logger, providerConfigs, defaultProvider);
        });

        services.AddScoped<IAIProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IAIProviderFactory>();
            return factory.GetDefaultProvider();
        });

        return services;
    }

    private static AIProviderConfig? ConfigureOpenAI(IConfiguration configuration)
    {
        var apiKey = GetApiKey(configuration["OpenAI:ApiKey"], "OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        return new AIProviderConfig
        {
            ProviderName = "OpenAI",
            ApiKey = apiKey,
            DefaultModel = configuration["OpenAI:DefaultModel"] ?? "gpt-4o-mini",
            TimeoutSeconds = int.TryParse(configuration["OpenAI:TimeoutSeconds"], out var timeout) ? timeout : 240
        };
    }

    private static AIProviderConfig? ConfigurePerplexity(IConfiguration configuration)
    {
        var apiKey = GetApiKey(configuration["Perplexity:ApiKey"], "FLIBUGET_AI_APIKEY");

        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        return new AIProviderConfig
        {
            ProviderName = "Perplexity",
            ApiKey = apiKey,
            BaseUrl = configuration["Perplexity:BaseUrl"] ?? "https://api.perplexity.ai",
            DefaultModel = configuration["Perplexity:DefaultModel"] ?? "sonar-pro",
            TimeoutSeconds = int.TryParse(configuration["Perplexity:TimeoutSeconds"], out var timeout) ? timeout : 240
        };
    }

    private static string? GetApiKey(string? configApiKey, string environmentVariableName)
    {
        if (!string.IsNullOrEmpty(configApiKey) && configApiKey != PLACEHOLDER_API_KEY)
            return configApiKey;

        return Environment.GetEnvironmentVariable(environmentVariableName);
    }
}
