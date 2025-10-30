using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;

namespace flibuget.Core.Examples;

/// <summary>
/// Example demonstrating how to use the AI infrastructure with multiple providers
/// </summary>
public class AIServiceUsageExample
{
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ILogger<AIServiceUsageExample> _logger;

  public AIServiceUsageExample(
     IAIProviderFactory aiProviderFactory,
   ILogger<AIServiceUsageExample> logger)
    {
     _aiProviderFactory = aiProviderFactory;
        _logger = logger;
    }

    /// <summary>
    /// Example: Simple question using the default provider
    /// </summary>
    public async Task SimpleQuestionExample()
{
        var provider = _aiProviderFactory.GetDefaultProvider();
   
        var response = await provider.AskAsync(
            "What are the top 5 science fiction books?",
  systemMessage: "You are a helpful book recommendation assistant.");
        
        _logger.LogInformation("Response: {Response}", response);
    }

    /// <summary>
    /// Example: Using a specific provider (OpenAI)
    /// </summary>
    public async Task OpenAIExample()
    {
        var openAI = _aiProviderFactory.CreateProvider("OpenAI");
        
        var response = await openAI.AskAsync(
      "Recommend an audiobook for someone who likes fantasy",
       model: "gpt-4o");
   
      _logger.LogInformation("OpenAI Response: {Response}", response);
    }

    /// <summary>
    /// Example: Using a specific provider (Perplexity)
    /// </summary>
    public async Task PerplexityExample()
    {
    var perplexity = _aiProviderFactory.CreateProvider("Perplexity");
    
 var response = await perplexity.AskAsync(
     "What is the latest bestselling audiobook?",
            model: "sonar-pro");
        
        _logger.LogInformation("Perplexity Response: {Response}", response);
    }

    /// <summary>
    /// Example: Advanced chat completion with multiple messages
    /// </summary>
    public async Task AdvancedChatExample()
    {
        var provider = _aiProviderFactory.GetDefaultProvider();
    
        var request = new AIChatCompletionRequest
        {
      Messages = new List<AIChatMessage>
            {
   new("system", "You are an expert audiobook curator with deep knowledge of narrators and audio productions."),
    new("user", "I loved listening to The Martian narrated by R.C. Bray. What similar audiobooks would you recommend?"),
        },
  Temperature = 0.7,
   MaxTokens = 500
        };

        var response = await provider.CreateChatCompletionAsync(request);
   
_logger.LogInformation("Chat Response: {Response}", 
            response.Choices[0].Message.Content);
        
        if (response.Usage != null)
        {
        _logger.LogInformation("Tokens used - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
    response.Usage.PromptTokens,
                response.Usage.CompletionTokens,
            response.Usage.TotalTokens);
        }
    }

    /// <summary>
    /// Example: Using in AudiobookService context
    /// </summary>
  public async Task AudiobookServiceExample()
    {
   // When injecting AudiobookService, it will use the default provider
      // var audiobookService = serviceProvider.GetRequiredService<AudiobookService>();
        
        // Simple query
        // var response = await audiobookService.AskAsync(
        //"Tell me about the audiobook 'Project Hail Mary' by Andy Weir");
        
    _logger.LogInformation("AudiobookService uses the default AI provider configured in appsettings.json");
    }

    /// <summary>
    /// Example: Comparing responses from different providers
    /// </summary>
    public async Task CompareProvidersExample()
    {
        var question = "What makes a great audiobook narration?";

   // Get response from OpenAI
        var openAI = _aiProviderFactory.CreateProvider("OpenAI");
 var openAIResponse = await openAI.AskAsync(question);
        _logger.LogInformation("OpenAI says: {Response}", openAIResponse);

 // Get response from Perplexity
        var perplexity = _aiProviderFactory.CreateProvider("Perplexity");
        var perplexityResponse = await perplexity.AskAsync(question);
        _logger.LogInformation("Perplexity says: {Response}", perplexityResponse);
    }

    /// <summary>
    /// Example: Error handling
    /// </summary>
    public async Task ErrorHandlingExample()
    {
        try
        {
     var provider = _aiProviderFactory.GetDefaultProvider();
      var response = await provider.AskAsync("");
        }
        catch (ArgumentException ex)
    {
      _logger.LogError(ex, "Invalid input provided");
        }
catch (InvalidOperationException ex)
        {
   _logger.LogError(ex, "Provider configuration error");
      }
        catch (Exception ex)
  {
    _logger.LogError(ex, "Unexpected error occurred");
     }
    }
}

/// <summary>
/// Configuration examples for appsettings.json
/// </summary>
public static class AIConfigurationExamples
{
    /// <summary>
/// Example configuration for OpenAI as default provider:
    /// 
    /// {
    ///   "AI": {
    ///     "DefaultProvider": "OpenAI"
    ///   },
  ///   "OpenAI": {
    ///     "ApiKey": "sk-...",
    ///     "DefaultModel": "gpt-4o-mini",
    ///     "TimeoutSeconds": 240
    ///   }
    /// }
    /// </summary>
    public static void OpenAIConfiguration() { }

    /// <summary>
    /// Example configuration for Perplexity as default provider:
    /// 
    /// {
    ///   "AI": {
    ///     "DefaultProvider": "Perplexity"
    ///   },
    ///   "Perplexity": {
    ///     "ApiKey": "pplx-...",
    ///   "DefaultModel": "sonar-pro",
    ///     "BaseUrl": "https://api.perplexity.ai",
    ///     "TimeoutSeconds": 240
    ///   }
    /// }
    /// </summary>
    public static void PerplexityConfiguration() { }

 /// <summary>
    /// Example configuration with multiple providers:
  /// 
    /// {
    ///   "AI": {
    /// "DefaultProvider": "OpenAI"
    ///   },
    ///   "OpenAI": {
    ///     "ApiKey": "sk-...",
    ///     "DefaultModel": "gpt-4o-mini"
    ///   },
    ///   "Perplexity": {
    ///     "ApiKey": "pplx-...",
    ///     "DefaultModel": "sonar-pro"
    ///   }
    /// }
    /// 
    /// Environment variables can also be used:
    /// - OPENAI_API_KEY for OpenAI
    /// - FLIBUGET_AI_APIKEY for Perplexity
    /// </summary>
    public static void MultiProviderConfiguration() { }
}
