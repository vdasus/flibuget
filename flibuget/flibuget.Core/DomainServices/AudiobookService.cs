using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;

namespace flibuget.Core.DomainServices
{
    /// <summary>
    /// Service for interacting with AI providers for audiobook-related queries
    /// </summary>
    public class AudiobookService
    {
        private readonly IAIProvider _aiProvider;
        private readonly ILogger<AudiobookService> _logger;

        public AudiobookService(IAIProvider aiProvider, ILogger<AudiobookService> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("AudiobookService initialized with provider: {Provider}", _aiProvider.ProviderName);
        }

        /// <summary>
        /// Creates a chat completion with the AI provider
        /// </summary>
        /// <param name="request">The chat completion request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Chat completion response</returns>
        public Task<AIChatCompletionResponse> CreateChatCompletionAsync(
            AIChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return _aiProvider.CreateChatCompletionAsync(request, cancellationToken);
        }

        /// <summary>
        /// Creates a simple chat completion with a single user message
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="systemMessage">Optional system message</param>
        /// <param name="model">Optional model override</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The content of the response</returns>
        public Task<string> AskAsync(
            string userMessage,
            string? systemMessage = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            return _aiProvider.AskAsync(userMessage, systemMessage, model, cancellationToken);
        }
    }
}
