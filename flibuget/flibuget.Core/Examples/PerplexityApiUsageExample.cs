using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.DependencyInjection;

namespace flibuget.Core.Examples;

/// <summary>
/// Examples demonstrating how to use the AI service for audiobook-related queries.
/// These examples cover common use cases and advanced scenarios.
/// </summary>
public static class AIServiceUsageExamplesForAudiobooks
{
    /// <summary>
    /// Example 1: Running all examples in sequence
    /// </summary>
    public static async Task RunAllExamples(IServiceProvider serviceProvider)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     AI Service Usage Examples - Complete Demo     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        await Example1_SimpleQuestionAndAnswerAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false); // Rate limiting

        await Example2_ConversationWithContextAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example4_ResearchQueryAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example5_WithCancellationTokenAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example6_ModelComparisonAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example7_TemperatureControlAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        await Example8_ErrorHandlingAsync(serviceProvider).ConfigureAwait(false);

        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   All examples completed!       ║");
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
             "What is the capital of France and what is it famous for?").ConfigureAwait(false);

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
            var request = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage>
   {
        new("system", "You are a helpful C# programming expert who explains concepts clearly with code examples."),
   new("user", "Explain dependency injection in .NET with a simple example.")
            },
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var response = await service.CreateChatCompletionAsync(request).ConfigureAwait(false);

            Console.WriteLine($"System: {request.Messages[0].Content}");
            Console.WriteLine($"User: {request.Messages[1].Content}");
            Console.WriteLine($"\nAssistant: {response.Choices[0].Message.Content}");
            Console.WriteLine($"\nTokens used: {response.Usage?.TotalTokens} (Prompt: {response.Usage?.PromptTokens}, Completion: {response.Usage?.CompletionTokens})\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 4: Research-focused query
    /// Use this for in-depth research queries that require current information.
    /// </summary>
    public static async Task Example4_ResearchQueryAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 4: Research Query ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();

        try
        {
            var request = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage>
     {
               new("system", "You are a research assistant. Provide well-sourced, detailed answers with current information."),
          new("user", "What are the latest developments in .NET 10 and what new features should developers be aware of?")
     },
                Temperature = 0.3, // Lower temperature for more factual responses
                MaxTokens = 1500
            };

            var response = await service.CreateChatCompletionAsync(request).ConfigureAwait(false);

            Console.WriteLine($"Research Query: {request.Messages[1].Content}\n");
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
    /// Example 6: Comparing different models
    /// Use this to understand the differences between models for cost/performance optimization.
    /// </summary>
    public static async Task Example6_ModelComparisonAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 6: Model Comparison ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "What is polymorphism in object-oriented programming?";

        try
        {
            // Test with fast model
            Console.WriteLine("Testing with fast model:");
            var response1 = await service.AskAsync(question, model: "gpt-4o-mini").ConfigureAwait(false);
            Console.WriteLine($"Response: {response1}\n");

            // Test with more capable model
            Console.WriteLine("Testing with more capable model:");
            var response2 = await service.AskAsync(question, model: "gpt-4o").ConfigureAwait(false);
            Console.WriteLine($"Response: {response2}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 7: Temperature control for creative vs factual responses
    /// Demonstrates how temperature affects response style.
    /// </summary>
    public static async Task Example7_TemperatureControlAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 7: Temperature Control ===\n");

        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "Explain what a variable is in programming.";

        try
        {
            // Low temperature (0.1) - More deterministic and factual
            Console.WriteLine("Low Temperature (0.1) - Factual and Precise:");
            var request1 = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage> { new("user", question) },
                Temperature = 0.1
            };
            var response1 = await service.CreateChatCompletionAsync(request1).ConfigureAwait(false);
            Console.WriteLine($"{response1.Choices[0].Message.Content}\n");

            // High temperature (0.9) - More creative and varied
            Console.WriteLine("High Temperature (0.9) - More Creative:");
            var request2 = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage> { new("user", question) },
                Temperature = 0.9
            };
            var response2 = await service.CreateChatCompletionAsync(request2).ConfigureAwait(false);
            Console.WriteLine($"{response2.Choices[0].Message.Content}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Example 8: Error handling best practices
    /// Demonstrates proper exception handling for various scenarios.
    /// </summary>
    public static async Task Example8_ErrorHandlingAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Example 8: Error Handling ===\n");

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

