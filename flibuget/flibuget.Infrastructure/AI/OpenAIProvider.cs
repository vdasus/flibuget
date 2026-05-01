using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace flibuget.Infrastructure.AI;

public class OpenAIProvider : IAIProvider
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly string _defaultModel;

    public string ProviderName => "OpenAI";

    public OpenAIProvider(AIProviderConfig config, ILogger<OpenAIProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(config));

        _defaultModel = config.DefaultModel ?? "gpt-4o-mini";
        _client = new OpenAIClient(new ApiKeyCredential(config.ApiKey));

        _logger.LogInformation("OpenAI provider initialized with model {Model}", _defaultModel);
    }

    public async Task<AIChatCompletionResponse> CreateChatCompletionAsync(
        AIChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Messages == null || request.Messages.Count == 0)
            throw new ArgumentException("Messages list cannot be empty", nameof(request));

        var model = request.Model ?? _defaultModel;
        var chatClient = _client.GetChatClient(model);

        var messages = new List<ChatMessage>();
        foreach (var msg in request.Messages)
        {
            messages.Add(msg.Role.ToLowerInvariant() switch
            {
                "system" => new SystemChatMessage(msg.Content),
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        var options = new ChatCompletionOptions
        {
            Temperature = (float?)request.Temperature,
            MaxOutputTokenCount = request.MaxTokens,
            TopP = (float?)request.TopP
        };

        _logger.LogInformation("Sending chat completion request to OpenAI with model {Model}", model);

        var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

        _logger.LogDebug("Received response from OpenAI");

        return new AIChatCompletionResponse
        {
            Id = response.Value.Id,
            Object = "chat.completion",
            Created = response.Value.CreatedAt.ToUnixTimeSeconds(),
            Model = response.Value.Model,
            Choices = new List<AIChoice>
            {
                new AIChoice
                {
                    Index = 0,
                    Message = new AIChatMessage { Role = "assistant", Content = response.Value.Content[0].Text },
                    FinishReason = response.Value.FinishReason.ToString()
                }
            },
            Usage = new AIUsage
            {
                PromptTokens = response.Value.Usage.InputTokenCount,
                CompletionTokens = response.Value.Usage.OutputTokenCount,
                TotalTokens = response.Value.Usage.TotalTokenCount
            }
        };
    }

    public async Task<string> AskAsync(
        string userMessage,
        string? systemMessage = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

        var messages = new List<AIChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemMessage))
            messages.Add(new AIChatMessage("system", systemMessage));

        messages.Add(new AIChatMessage("user", userMessage));

        var request = new AIChatCompletionRequest { Messages = messages, Model = model };
        var response = await CreateChatCompletionAsync(request, cancellationToken);

        if (response.Choices.Count == 0)
            throw new InvalidOperationException("No choices returned from OpenAI");

        return response.Choices[0].Message.Content;
    }
}
