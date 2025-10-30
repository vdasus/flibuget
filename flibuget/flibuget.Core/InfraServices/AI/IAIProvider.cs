namespace flibuget.Core.InfraServices.AI;

/// <summary>
/// Interface for AI provider implementations
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Gets the name of the AI provider (e.g., "OpenAI", "Perplexity")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a chat completion with the AI provider
    /// </summary>
    /// <param name="request">The chat completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat completion response</returns>
    Task<AIChatCompletionResponse> CreateChatCompletionAsync(
        AIChatCompletionRequest request,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a simple chat completion with a single user message
    /// </summary>
    /// <param name="userMessage">The user's message</param>
    /// <param name="systemMessage">Optional system message</param>
 /// <param name="model">Optional model override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The content of the response</returns>
    Task<string> AskAsync(
        string userMessage,
        string? systemMessage = null,
        string? model = null,
  CancellationToken cancellationToken = default);
}
