using flibuget.Core.InfraServices;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using flibuget.Core.Domain.DTO;

namespace flibuget.Core.DomainServices
{
    /// <summary>
    /// Service for interacting with Perplexity API
    /// </summary>
    public class AudiobookService
    {
        private readonly IWebService _webService;
        private readonly ILogger<AudiobookService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.perplexity.ai";

        public AudiobookService(IWebService webService, ILogger<AudiobookService> logger, string apiKey)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            _apiKey = apiKey;
        }

        /// <summary>
        /// Creates a chat completion with the Perplexity API
        /// </summary>
        /// <param name="messages">List of messages in the conversation</param>
        /// <param name="model">Model to use (default: sonar-pro)</param>
        /// <param name="responseFormat">Optional response format configuration for structured output</param>
        /// <param name="temperature">Optional temperature parameter</param>
        /// <param name="maxTokens">Optional max tokens parameter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Chat completion response</returns>
        public async Task<PerplexityChatCompletionResponse> CreateChatCompletionAsync(
            List<PerplexityMessage> messages,
            string model = "sonar-pro",
            PerplexityResponseFormat? responseFormat = null,
            double? temperature = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messages);

            if (messages.Count == 0)
                throw new ArgumentException("Messages list cannot be empty", nameof(messages));

            var request = new PerplexityChatCompletionRequest
            {
                Messages = messages,
                Model = model,
                ResponseFormat = responseFormat,
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            var uri = new Uri($"{_baseUrl}/chat/completions");
            var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogInformation("Sending chat completion request to Perplexity API with model {Model}", model);

            var responseJson = await _webService.MakeJsonPostRequestWithBearerAsync(
                uri,
                jsonRequest,
                _apiKey,
                timeout: 240,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Received response from Perplexity API");

            var response = JsonSerializer.Deserialize<PerplexityChatCompletionResponse>(responseJson);

            if (response == null)
                throw new InvalidOperationException("Failed to deserialize Perplexity API response");

            return response;
        }

        /// <summary>
        /// Creates a simple chat completion with a single user message
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="model">Model to use (default: sonar-pro)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The content of the first choice response</returns>
        public async Task<string> AskAsync(
            string userMessage,
            string model = "sonar-pro",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

            var messages = new List<PerplexityMessage>
            {
                new() { Role = "user", Content = userMessage }
            };

            var response = await CreateChatCompletionAsync(messages, model, cancellationToken: cancellationToken);

            if (response.Choices.Count == 0)
                throw new InvalidOperationException("No choices returned from Perplexity API");

            return response.Choices[0].Message.Content;
        }

        /// <summary>
        /// Creates a chat completion with structured JSON response
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="jsonSchema">The JSON schema for the structured response</param>
        /// <param name="model">Model to use (default: sonar-pro)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Chat completion response with structured data</returns>
        public async Task<PerplexityChatCompletionResponse> CreateStructuredCompletionAsync(
            string userMessage,
            PerplexityJsonSchema jsonSchema,
            string model = "sonar-pro",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

            ArgumentNullException.ThrowIfNull(jsonSchema);

            var messages = new List<PerplexityMessage>
            {
                new() { Role = "user", Content = userMessage }
            };

            var responseFormat = new PerplexityResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new PerplexityJsonSchemaWrapper
                {
                    Schema = jsonSchema
                }
            };

            return await CreateChatCompletionAsync(
                messages,
                model,
                responseFormat,
                cancellationToken: cancellationToken);
        }
    }
}
