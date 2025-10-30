# AI Infrastructure Service

This project includes a flexible AI infrastructure service that supports multiple AI providers including OpenAI and Perplexity.

## Features

- **Multi-Provider Support**: Seamlessly switch between OpenAI, Perplexity, and potentially other providers
- **Factory Pattern**: Easy provider instantiation and management
- **Unified Interface**: Common API across all providers
- **Configuration-Based**: Configure providers via appsettings.json or environment variables
- **Type-Safe**: Strongly-typed models and responses
- **Logging**: Comprehensive logging support
- **Extensible**: Easy to add new AI providers

## Supported Providers

### OpenAI
- Uses Azure.AI.OpenAI SDK
- Default model: `gpt-4o-mini`
- Supported models: `gpt-4o`, `gpt-4o-mini`, `gpt-4-turbo`, `gpt-3.5-turbo`
- Environment variable: `OPENAI_API_KEY`

### Perplexity
- Uses custom HTTP implementation
- Default model: `sonar-pro`
- Supported models: `sonar-pro`, `sonar`, `sonar-reasoning`
- Environment variable: `FLIBUGET_AI_APIKEY`

## Configuration

### appsettings.json

```json
{
  "AI": {
    "DefaultProvider": "OpenAI"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here",
    "DefaultModel": "gpt-4o-mini",
    "TimeoutSeconds": 240
  },
  "Perplexity": {
    "ApiKey": "your-perplexity-api-key-here",
    "DefaultModel": "sonar-pro",
    "BaseUrl": "https://api.perplexity.ai",
    "TimeoutSeconds": 240
  }
}
```

### Environment Variables

Instead of hardcoding API keys in appsettings.json, you can use environment variables (recommended):

- **OpenAI**: Set `OPENAI_API_KEY`
- **Perplexity**: Set `FLIBUGET_AI_APIKEY`

The service will automatically use environment variables if the appsettings.json value is a placeholder.

## Usage

### Basic Usage with Default Provider

```csharp
public class MyService
{
    private readonly IAIProvider _aiProvider;

    public MyService(IAIProvider aiProvider)
    {
    _aiProvider = aiProvider;
    }

    public async Task<string> GetRecommendation()
    {
        var response = await _aiProvider.AskAsync(
  "Recommend a science fiction audiobook",
   systemMessage: "You are a helpful book recommendation assistant");

        return response;
    }
}
```

### Using Specific Provider

```csharp
public class MyService
{
    private readonly IAIProviderFactory _factory;

    public MyService(IAIProviderFactory factory)
    {
        _factory = factory;
    }

    public async Task CompareProviders()
    {
        // Use OpenAI
        var openAI = _factory.CreateProvider("OpenAI");
        var openAIResponse = await openAI.AskAsync("What is AI?");

        // Use Perplexity
   var perplexity = _factory.CreateProvider("Perplexity");
        var perplexityResponse = await perplexity.AskAsync("What is AI?");
    }
}
```

### Advanced Chat Completion

```csharp
var request = new AIChatCompletionRequest
{
    Messages = new List<AIChatMessage>
    {
        new("system", "You are an expert audiobook curator"),
        new("user", "Recommend audiobooks similar to The Martian")
    },
    Temperature = 0.7,
    MaxTokens = 500,
    Model = "gpt-4o" // Optional model override
};

var response = await _aiProvider.CreateChatCompletionAsync(request);
Console.WriteLine(response.Choices[0].Message.Content);

// Check token usage
if (response.Usage != null)
{
    Console.WriteLine($"Tokens used: {response.Usage.TotalTokens}");
}
```

### Using AudiobookService

The `AudiobookService` uses the AI infrastructure:

```csharp
public class MyController
{
    private readonly AudiobookService _audiobookService;

    public MyController(AudiobookService audiobookService)
    {
        _audiobookService = audiobookService;
    }

    public async Task<string> SearchAudiobook(string query)
    {
     // Uses the default AI provider configured in appsettings.json
        var result = await _audiobookService.AskAsync(query);
        return result;
    }

    public async Task<AIChatCompletionResponse> DetailedSearch(string author, string title)
    {
        var request = new AIChatCompletionRequest
        {
       Messages = new List<AIChatMessage>
     {
            new("system", "You are an audiobook expert"),
                new("user", $"Find information about '{title}' by {author}")
        },
    Temperature = 0.3,
    MaxTokens = 1000
        };

        return await _audiobookService.CreateChatCompletionAsync(request);
 }
}
```

## Architecture

### Interfaces

- **`IAIProvider`**: Core interface for AI provider implementations
  - `AskAsync()`: Simple question-answer method
  - `CreateChatCompletionAsync()`: Advanced chat completion with full control
  - `ProviderName`: Read-only property returning provider name

- **`IAIProviderFactory`**: Factory for creating provider instances
  - `CreateProvider(string)`: Create a specific provider
  - `GetDefaultProvider()`: Get the default configured provider

### Implementations

- **`OpenAIProvider`**: OpenAI implementation using Azure.AI.OpenAI SDK
- **`PerplexityProvider`**: Perplexity implementation using HTTP client
- **`AIProviderFactory`**: Factory implementation managing provider lifecycle

### Models

- **`AIChatMessage`**: Represents a chat message with role and content
- **`AIChatCompletionRequest`**: Request model with messages, temperature, max tokens, etc.
- **`AIChatCompletionResponse`**: Response model with choices, usage info, and metadata
- **`AIProviderConfig`**: Configuration for a provider (API key, model, timeout)
- **`AIUsage`**: Token usage information (prompt, completion, total)
- **`AIChoice`**: Individual response choice with message and finish reason

## Dependency Injection Setup

The services are registered in `CompositionRoot.cs`:

```csharp
// Add AI Services (supports OpenAI, Perplexity, etc.)
services.AddAIServices(configuration);

// Register AudiobookService (uses AI infrastructure)
services.AddScoped<AudiobookService>();
```

## Adding New Providers

To add a new AI provider (e.g., Anthropic Claude):

1. **Create Provider Class** implementing `IAIProvider`:

```csharp
public class AnthropicProvider : IAIProvider
{
    public string ProviderName => "Anthropic";

  public async Task<AIChatCompletionResponse> CreateChatCompletionAsync(
AIChatCompletionRequest request,
        CancellationToken cancellationToken = default)
 {
    // Implementation here
    }

    public async Task<string> AskAsync(
        string userMessage,
        string? systemMessage = null,
        string? model = null,
     CancellationToken cancellationToken = default)
    {
        // Implementation here
    }
}
```

2. **Update Factory** in `AIProviderFactory.CreateProvider`:

```csharp
return providerName.ToLowerInvariant() switch
{
    "openai" => new OpenAIProvider(config, GetService<ILogger<OpenAIProvider>>()),
    "perplexity" => new PerplexityProvider(config, GetService<IWebService>(), GetService<ILogger<PerplexityProvider>>()),
    "anthropic" => new AnthropicProvider(config, GetService<ILogger<AnthropicProvider>>()),
    _ => throw new NotSupportedException($"Provider '{providerName}' is not supported")
};
```

3. **Add Configuration** in `AIServiceExtensions`:

```csharp
private static AIProviderConfig? ConfigureAnthropic(IConfiguration configuration)
{
    var apiKey = GetApiKey(configuration["Anthropic:ApiKey"], "ANTHROPIC_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
        return null;

 return new AIProviderConfig
    {
        ProviderName = "Anthropic",
      ApiKey = apiKey,
        DefaultModel = configuration["Anthropic:DefaultModel"] ?? "claude-3-opus-20240229",
  TimeoutSeconds = int.TryParse(configuration["Anthropic:TimeoutSeconds"], out var timeout) ? timeout : 240
    };
}
```

4. **Update appsettings.json**:

```json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key-here",
    "DefaultModel": "claude-3-opus-20240229"
  }
}
```

## Best Practices

1. **Use Dependency Injection**: Inject `IAIProvider` or `IAIProviderFactory`
2. **Configure via Environment Variables**: Keep API keys secure, never commit them
3. **Handle Errors**: Wrap API calls in try-catch blocks
4. **Log Appropriately**: Use the built-in logging for debugging and monitoring
5. **Monitor Token Usage**: Check `response.Usage` to control costs
6. **Use System Messages**: Provide context to improve response quality
7. **Set Appropriate Timeouts**: Adjust based on expected response times
8. **Choose Right Model**: Balance cost vs. capability
9. **Control Temperature**: Lower (0.1-0.3) for facts, higher (0.7-0.9) for creativity
10. **Implement Retry Logic**: Handle transient network failures gracefully

## Examples

See `flibuget.Core\Examples\AIServiceUsageExample.cs` for comprehensive usage examples including:
- Simple questions
- Multi-turn conversations
- Structured JSON responses
- Temperature control
- Error handling
- Cancellation tokens
- Model comparison

## NuGet Packages

- `Azure.AI.OpenAI` (v2.1.0) - OpenAI SDK
- `Microsoft.Extensions.Logging` (v9.0.10) - Logging support
- `Microsoft.Extensions.Configuration` (v9.0.10) - Configuration support
- `Microsoft.Extensions.Http` (v9.0.10) - HTTP client factory

## Performance Considerations

### Token Optimization
```csharp
// Limit response length to control cost
var request = new AIChatCompletionRequest
{
    Messages = messages,
    MaxTokens = 500  // Adjust based on needs
};
```

### Timeouts
```csharp
// Set appropriate timeouts in configuration
"TimeoutSeconds": 60  // Shorter for simple queries
"TimeoutSeconds": 240 // Longer for complex tasks
```

### Caching
Consider caching frequently asked questions to reduce API calls.

## Security

- **Never commit API keys** to version control
- **Use environment variables** in production
- **Rotate keys regularly**
- **Monitor usage** for unexpected spikes
- **Implement rate limiting** in your application
- **Validate user input** before sending to AI

## Troubleshooting

### Provider Not Configured
**Error**: "Provider 'X' is not configured"
**Solution**: Check that the provider has a valid API key in appsettings.json or environment variable

### API Key Missing
**Error**: "API key cannot be null or empty"
**Solution**: Set the appropriate environment variable or update appsettings.json

### Model Not Found
**Error**: Model name not recognized
**Solution**: Verify the model name matches the provider's supported models

### Timeout Issues
**Error**: Request times out
**Solution**: Increase `TimeoutSeconds` in configuration or optimize your prompt

## License

Part of the flibuget project.
