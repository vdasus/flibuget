using flibuget.Core.Domain.DTO;
using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace flibuget.Infrastructure.AI;

public class PerplexityProvider : IAIProvider
{
    private readonly IWebService _webService;
    private readonly ILogger<PerplexityProvider> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public string ProviderName => "Perplexity";

    public PerplexityProvider(AIProviderConfig config, IWebService webService, ILogger<PerplexityProvider> logger)
    {
        _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(config));

        _apiKey = config.ApiKey;
        _baseUrl = config.BaseUrl ?? "https://api.perplexity.ai";
        _defaultModel = config.DefaultModel ?? "sonar-pro";

        _logger.LogInformation("Perplexity provider initialized with model {Model}", _defaultModel);
    }

    public async Task<AIChatCompletionResponse> CreateChatCompletionAsync(
        AIChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Messages == null || request.Messages.Count == 0)
            throw new ArgumentException("Messages list cannot be empty", nameof(request));

        var perplexityMessages = request.Messages.Select(m => new PerplexityMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        var perplexityRequest = new PerplexityChatCompletionRequest
        {
            Messages = perplexityMessages,
            Model = request.Model ?? _defaultModel,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            TopP = request.TopP,
            Stream = request.Stream,
            ResponseFormat = request.ResponseFormat as PerplexityResponseFormat
        };

        var uri = new Uri($"{_baseUrl}/chat/completions");
        var jsonRequest = JsonSerializer.Serialize(perplexityRequest, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        _logger.LogInformation("Sending chat completion request to Perplexity API with model {Model}", perplexityRequest.Model);

        var responseJson = await _webService.MakeJsonPostRequestWithBearerAsync(
            uri, jsonRequest, _apiKey, timeout: 240, cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Received response from Perplexity API");

        var perplexityResponse = JsonSerializer.Deserialize<PerplexityChatCompletionResponse>(responseJson);

        if (perplexityResponse == null)
            throw new InvalidOperationException("Failed to deserialize Perplexity API response");

        return new AIChatCompletionResponse
        {
            Id = perplexityResponse.Id,
            Object = perplexityResponse.Object,
            Created = perplexityResponse.Created,
            Model = perplexityResponse.Model,
            Choices = perplexityResponse.Choices.Select(c => new AIChoice
            {
                Index = c.Index,
                Message = new AIChatMessage { Role = c.Message.Role, Content = c.Message.Content },
                FinishReason = c.FinishReason
            }).ToList(),
            Usage = perplexityResponse.Usage != null ? new AIUsage
            {
                PromptTokens = perplexityResponse.Usage.PromptTokens,
                CompletionTokens = perplexityResponse.Usage.CompletionTokens,
                TotalTokens = perplexityResponse.Usage.TotalTokens
            } : null
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
        var response = await CreateChatCompletionAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.Choices.Count == 0)
            throw new InvalidOperationException("No choices returned from Perplexity API");

        return response.Choices[0].Message.Content;
    }
}
