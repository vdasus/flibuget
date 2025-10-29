using System.Text.Json;
using flibuget.Core.Domain.DTO;
using flibuget.Core.DomainServices;
using Microsoft.Extensions.DependencyInjection;

namespace flibuget.Core.Examples;

/// <summary>
/// Comprehensive examples demonstrating how to use the Perplexity API client (AudiobookService).
/// These examples cover common use cases and advanced scenarios.
/// </summary>
public static class PerplexityApiUsageExample
{
    /// <summary>
    /// Example 10: Running all examples in sequence
    /// </summary>
    public static async Task RunAllExamples(IServiceProvider serviceProvider)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Perplexity API Usage Examples - Complete Demo      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        await Example1_SimpleQuestionAndAnswerAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false); // Rate limiting

        await Example2_ConversationWithContextAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example3_AudioBookStructuredJsonResponseAsync(serviceProvider, "Хоббит", "Дж. Р. Р. Толкиен").ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example4_ResearchQueryAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example5_WithCancellationTokenAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example6_ModelComparisonAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example8_TemperatureControlAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example9_ErrorHandlingAsync(serviceProvider).ConfigureAwait(false);

        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   All examples completed!                                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    }

    /// <summary>
    /// Example 1: Simple question and answer
    /// Use this for straightforward queries that don't require conversation context.
    /// </summary>
    public static async Task Example1_SimpleQuestionAndAnswerAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 1: Simple Question & Answer ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        try
        {
            var answer = await service.AskAsync(
                "What is the capital of France and what is it famous for?",
                model: "sonar-pro").ConfigureAwait(false);

            Console.WriteLine("Question: What is the capital of France and what is it famous for?");
            Console.WriteLine($"Answer: {answer}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 2: Multi-turn conversation with system prompt
    /// Use this when you need context or want to set the AI's behavior/role.
    /// </summary>
    public static async Task Example2_ConversationWithContextAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 2: Multi-turn Conversation ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        try
        {
            var messages = new List<PerplexityMessage>
            {
    new() { Role = "system", Content = "You are a helpful C# programming expert who explains concepts clearly with code examples." },
      new() { Role = "user", Content = "Explain dependency injection in .NET with a simple example." }
        };

            var response = await service.CreateChatCompletionAsync(
                messages,
                model: "sonar-pro",
                temperature: 0.7,
                maxTokens: 1000).ConfigureAwait(false);

            Console.WriteLine($"System: {messages[0].Content}");
            Console.WriteLine($"User: {messages[1].Content}");
            Console.WriteLine($"\nAssistant: {response.Choices[0].Message.Content}");
            Console.WriteLine($"\nTokens used: {response.Usage?.TotalTokens} (Prompt: {response.Usage?.PromptTokens}, Completion: {response.Usage?.CompletionTokens})\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 3: Structured JSON response with schema - Audiobook Description
    /// Use this when you need predictable, structured data that can be deserialized into C# objects.
    /// This example demonstrates getting detailed audiobook information in a structured format.
    /// </summary>
    public static async Task<AudiobookDescriptionDto> Example3_AudioBookStructuredJsonResponseAsync(IServiceProvider serviceProvider, string book, string author)
    {
        Console.WriteLine("=== Example 3: Structured JSON Response - Audiobook Description ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        try
        {
            // Define the JSON schema for audiobook description
            var schema = new PerplexityJsonSchema
            {
                Type = "object",
                Properties = new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["author"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["cover_link"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["description"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["themes"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["items"] = new Dictionary<string, string> { ["type"] = "string" }
                    },
                    ["duration"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["narrator"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["age_restriction"] = new Dictionary<string, string> { ["type"] = "string" },
                    ["link"] = new Dictionary<string, string> { ["type"] = "string" }
                },
                Required = ["title", "author", "cover_link", "description", "themes", "duration", "narrator", "age_restriction", "link"]
            };

            // Create the prompt in Russian as specified
            var prompt = @$"Пожалуйста, предоставь подробное структурированное описание аудиокниги в формате JSON, со следующими полями:

- title: название книги,
- author: автор,
- cover_link: прямая ссылка на обложку книги в высоком разрешении,
- description: краткое, но ёмкое описание сюжета аудиокниги, включая жанры и основные темы,
- themes: список основных тем или жанров книги (например, ""фэнтези"", ""приключения"", ""юмор""),
- duration: длительность аудиокниги (часы и минуты),
- narrator: имя чтеца (если известно),
- age_restriction: возрастные ограничения (если есть),
- link: ссылка на официальный или крупный ресурс, где можно прослушать или приобрести аудиокнигу.

Описание должно быть информативным и привлекательным, отражать атмосферу и цель произведения. Поля должны быть заполнены максимально полно.

Пожалуйста, составь такой JSON для книги ""{book}"" {author}.";

            var completion = await service.CreateStructuredCompletionAsync(
                 prompt,
                    schema,
           model: "sonar-pro").ConfigureAwait(false);

            Console.WriteLine("Structured JSON Response:");
            Console.WriteLine(completion.Choices[0].Message.Content);

            // Deserialize to strongly-typed object
            var result = JsonSerializer.Deserialize<AudiobookDescriptionDto>(
             completion.Choices[0].Message.Content);

            Console.WriteLine("\n=== Parsed Audiobook Information ===");
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
            return new AudiobookDescriptionDto();
        }
        return new AudiobookDescriptionDto();
    }

    /// <summary>
    /// Example 4: Research-focused query with sonar-pro model
    /// Use this for in-depth research queries that require current information.
    /// </summary>
    public static async Task Example4_ResearchQueryAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 4: Research Query ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        try
        {
            var messages = new List<PerplexityMessage>
            {
  new()
      {
  Role = "system",
           Content = "You are a research assistant. Provide well-sourced, detailed answers with current information."
    },
      new()
     {
 Role = "user",
         Content = "What are the latest developments in .NET 9 and what new features should developers be aware of?"
     }
   };

            var response = await service.CreateChatCompletionAsync(
                messages,
                model: "sonar-pro",
                temperature: 0.3, // Lower temperature for more factual responses
                maxTokens: 1500).ConfigureAwait(false);

            Console.WriteLine($"Research Query: {messages[1].Content}\n");
            Console.WriteLine($"Response:\n{response.Choices[0].Message.Content}\n");
            Console.WriteLine($"Model used: {response.Model}");
            Console.WriteLine($"Finish reason: {response.Choices[0].FinishReason}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 5: Using cancellation tokens for timeout control
    /// Use this when you need to limit the time spent waiting for a response.
    /// </summary>
    public static async Task Example5_WithCancellationTokenAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 5: With Cancellation Token ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            Console.WriteLine("Sending request with 30-second timeout...");

            var answer = await service.AskAsync(
                "Explain the concept of async/await in C# and how it improves application performance.",
                model: "sonar-pro",
                cancellationToken: cts.Token).ConfigureAwait(false);

            Console.WriteLine($"Response received:\n{answer}\n");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request was cancelled due to timeout.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 6: Comparing sonar vs sonar-pro models
    /// Use this to understand the differences between models for cost/performance optimization.
    /// </summary>
    public static async Task Example6_ModelComparisonAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 6: Model Comparison (sonar vs sonar-pro) ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "What is polymorphism in object-oriented programming?";

        try
        {
            // Test with sonar (faster, cost-effective)
            Console.WriteLine("Testing with 'sonar' model:");
            var sonarResponse = await service.AskAsync(question, model: "sonar").ConfigureAwait(false);
            Console.WriteLine($"Response: {sonarResponse}\n");

            // Test with sonar-pro (more capable)
            Console.WriteLine("Testing with 'sonar-pro' model:");
            var sonarProResponse = await service.AskAsync(question, model: "sonar-pro").ConfigureAwait(false);
            Console.WriteLine($"Response: {sonarProResponse}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 8: Temperature control for creative vs factual responses
    /// Demonstrates how temperature affects response style.
    /// </summary>
    public static async Task Example8_TemperatureControlAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 8: Temperature Control ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "Explain what a variable is in programming.";

        try
        {
            // Low temperature (0.1) - More deterministic and factual
            Console.WriteLine("Low Temperature (0.1) - Factual and Precise:");
            var messages1 = new List<PerplexityMessage>
      {
  new() { Role = "user", Content = question }
            };
            var response1 = await service.CreateChatCompletionAsync(
                messages1,
                model: "sonar-pro",
                temperature: 0.1).ConfigureAwait(false);
            Console.WriteLine($"{response1.Choices[0].Message.Content}\n");

            // High temperature (0.9) - More creative and varied
            Console.WriteLine("High Temperature (0.9) - More Creative:");
            var messages2 = new List<PerplexityMessage>
   {
                new() { Role = "user", Content = question }
            };
            var response2 = await service.CreateChatCompletionAsync(
                messages2,
                model: "sonar-pro",
                temperature: 0.9).ConfigureAwait(false);
            Console.WriteLine($"{response2.Choices[0].Message.Content}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 9: Error handling best practices
    /// Demonstrates proper exception handling for various scenarios.
    /// </summary>
    public static async Task Example9_ErrorHandlingAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 9: Error Handling ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        // Example 1: Invalid input
        try
        {
            Console.WriteLine("Test 1: Empty message (should throw ArgumentException)");
            await service.AskAsync("").ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✓ Caught expected exception: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}\n");
        }

        // Example 2: Network timeout
        try
        {
            Console.WriteLine("Test 2: Request with very short timeout (may timeout)");
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            await service.AskAsync("What is AI?", cancellationToken: cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✓ Request was cancelled as expected\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Different exception: {ex.GetType().Name}: {ex.Message}\n");
        }

        // Example 3: General error handling pattern
        try
        {
            Console.WriteLine("Test 3: Normal request with comprehensive error handling");
            var response = await service.AskAsync("What is machine learning?").ConfigureAwait(false);
            Console.WriteLine($"✓ Success: {response[..Math.Min(100, response.Length)]}...\n");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid argument: {ex.Message}\n");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}\n");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"Request timed out: {ex.Message}\n");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invalid operation: {ex.Message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.GetType().Name}: {ex.Message}\n");
        }
    }

    
}

