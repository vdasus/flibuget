using flibuget.Core.Domain.DTO;
using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Gets audiobook information from AI based on book title, author, and narrator
        /// </summary>
        /// <param name="book">Book title</param>
        /// <param name="author">Author name</param>
        /// <param name="narrator">Narrator name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Structured audiobook description</returns>
        /// <exception cref="InvalidOperationException">Thrown when AI response cannot be parsed</exception>
        public async Task<AudiobookDescriptionDto> GetAudiobookInfoFromAIAsync(
            string book, 
            string author, 
            string narrator,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Requesting audiobook info for: {Book} by {Author} (narrator: {Narrator})", 
                book, author, narrator);

            var prompt = @$"Please provide a detailed structured description of the audiobook in JSON format with the following fields:

- title: book title,
- author: author name,
- cover_link: direct link to the book cover in high resolution,
- description: brief but comprehensive description of the audiobook plot, including genres and main themes,
- themes: list of main themes or genres of the book (e.g., ""fantasy"", ""adventure"", ""humor""),
- duration: audiobook duration (hours and minutes),
- narrator: name of the narrator (if known),
- year: year of publication,
- age_restriction: age restrictions (if any),
- link: link to an official or major resource where you can listen to or purchase the audiobook.

The description should be informative and engaging, reflecting the atmosphere and purpose of the work. Fields should be filled as completely as possible.

Please compose such JSON for the book ""{book}"" by {author} with narrator {narrator}.

Return ONLY the JSON object, no additional text.";

            var answer = await AskAsync(
                    prompt,
                    systemMessage: "You are a helpful assistant that returns only valid JSON responses.",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Strip markdown code blocks if present
            var cleanJson = StripMarkdownCodeBlocks(answer);

            try
            {
                // Deserialize to strongly-typed object
                var result = JsonSerializer.Deserialize<AudiobookDescriptionDto>(cleanJson);
                
                if (result == null)
                {
                    _logger.LogError("Failed to deserialize audiobook info response");
                    throw new InvalidOperationException("Can't get info from AI.");
                }

                _logger.LogInformation("Successfully retrieved audiobook info for: {Title}", result.Title);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", cleanJson);
                throw new InvalidOperationException("Can't get info from AI.", ex);
            }
        }

        /// <summary>
        /// Strips markdown code blocks from AI responses to extract pure JSON.
        /// Handles both ```json and ``` code block formats.
        /// </summary>
        /// <param name="response">The AI response that may contain markdown-wrapped JSON</param>
        /// <returns>Clean JSON string</returns>
        private static string StripMarkdownCodeBlocks(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            // Remove markdown code blocks: ```json ... ``` or ``` ... ```
            var pattern = @"^```(?:json)?\s*\n?(.*?)\n?```$";
            var match = Regex.Match(response.Trim(), pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value.Trim() : response.Trim();
        }
    }
}
