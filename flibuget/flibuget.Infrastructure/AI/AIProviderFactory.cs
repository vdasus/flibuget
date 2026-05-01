using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;

namespace flibuget.Infrastructure.AI;

public interface IAIProviderFactory
{
    IAIProvider CreateProvider(string providerName);
    IAIProvider GetDefaultProvider();
}

public class AIProviderFactory(
    IServiceProvider serviceProvider,
    ILogger<AIProviderFactory> logger,
    Dictionary<string, AIProviderConfig> providerConfigs,
    string defaultProvider = "OpenAI")
    : IAIProviderFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<AIProviderFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<string, AIProviderConfig> _providerConfigs = providerConfigs ?? throw new ArgumentNullException(nameof(providerConfigs));

    public IAIProvider CreateProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

        if (!_providerConfigs.TryGetValue(providerName, out var config))
            throw new InvalidOperationException($"Provider '{providerName}' is not configured");

        _logger.LogInformation("Creating AI provider: {ProviderName}", providerName);

        return providerName.ToLowerInvariant() switch
        {
            "openai" => new OpenAIProvider(config, GetService<ILogger<OpenAIProvider>>()),
            "perplexity" => new PerplexityProvider(config, GetService<IWebService>(), GetService<ILogger<PerplexityProvider>>()),
            _ => throw new NotSupportedException($"Provider '{providerName}' is not supported")
        };
    }

    public IAIProvider GetDefaultProvider() => CreateProvider(defaultProvider);

    private T GetService<T>() where T : notnull
    {
        return (T)(_serviceProvider.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service of type {typeof(T)} is not registered"));
    }
}
