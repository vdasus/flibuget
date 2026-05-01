using flibuget.Core.InfraServices.AI;
using flibuget.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace flibuget.Infrastructure.Examples;

public class AIServiceUsageExample
{
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly ILogger<AIServiceUsageExample> _logger;

    public AIServiceUsageExample(IAIProviderFactory aiProviderFactory, ILogger<AIServiceUsageExample> logger)
    {
        _aiProviderFactory = aiProviderFactory;
        _logger = logger;
    }

    public async Task SimpleQuestionExample()
    {
        var provider = _aiProviderFactory.GetDefaultProvider();
        var response = await provider.AskAsync(
            "What are the top 5 science fiction books?",
            systemMessage: "You are a helpful book recommendation assistant.");
        _logger.LogInformation("Response: {Response}", response);
    }

    public async Task OpenAIExample()
    {
        var openAI = _aiProviderFactory.CreateProvider("OpenAI");
        var response = await openAI.AskAsync("Recommend an audiobook for someone who likes fantasy", model: "gpt-4o");
        _logger.LogInformation("OpenAI Response: {Response}", response);
    }

    public async Task PerplexityExample()
    {
        var perplexity = _aiProviderFactory.CreateProvider("Perplexity");
        var response = await perplexity.AskAsync("What is the latest bestselling audiobook?", model: "sonar-pro");
        _logger.LogInformation("Perplexity Response: {Response}", response);
    }

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
        _logger.LogInformation("Chat Response: {Response}", response.Choices[0].Message.Content);
    }

    public async Task CompareProvidersExample()
    {
        var question = "What makes a great audiobook narration?";

        var openAI = _aiProviderFactory.CreateProvider("OpenAI");
        var openAIResponse = await openAI.AskAsync(question);
        _logger.LogInformation("OpenAI says: {Response}", openAIResponse);

        var perplexity = _aiProviderFactory.CreateProvider("Perplexity");
        var perplexityResponse = await perplexity.AskAsync(question);
        _logger.LogInformation("Perplexity says: {Response}", perplexityResponse);
    }

    public async Task ErrorHandlingExample()
    {
        try
        {
            var provider = _aiProviderFactory.GetDefaultProvider();
            await provider.AskAsync("");
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
