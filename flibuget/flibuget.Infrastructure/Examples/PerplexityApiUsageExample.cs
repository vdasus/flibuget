using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices.AI;
using Microsoft.Extensions.DependencyInjection;

namespace flibuget.Infrastructure.Examples;

public static class AIServiceUsageExamplesForAudiobooks
{
    public static async Task RunAllExamples(IServiceProvider serviceProvider)
    {
        await Example1_SimpleQuestionAndAnswerAsync(serviceProvider).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);
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
    }

    public static async Task Example1_SimpleQuestionAndAnswerAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        try
        {
            var answer = await service.AskAsync("What is the capital of France and what is it famous for?").ConfigureAwait(false);
            Console.WriteLine($"Answer: {answer}");
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example2_ConversationWithContextAsync(IServiceProvider serviceProvider)
    {
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
            Console.WriteLine($"Assistant: {response.Choices[0].Message.Content}");
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example4_ResearchQueryAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        try
        {
            var request = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage>
                {
                    new("system", "You are a research assistant. Provide well-sourced, detailed answers with current information."),
                    new("user", "What are the latest developments in .NET 10?")
                },
                Temperature = 0.3,
                MaxTokens = 1500
            };
            var response = await service.CreateChatCompletionAsync(request).ConfigureAwait(false);
            Console.WriteLine($"Response: {response.Choices[0].Message.Content}");
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example5_WithCancellationTokenAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var answer = await service.AskAsync("Explain async/await in C#.", cancellationToken: cts.Token).ConfigureAwait(false);
            Console.WriteLine($"Response: {answer}");
        }
        catch (OperationCanceledException) { Console.WriteLine("Request was cancelled due to timeout."); }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example6_ModelComparisonAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "What is polymorphism in object-oriented programming?";
        try
        {
            var response1 = await service.AskAsync(question, model: "gpt-4o-mini").ConfigureAwait(false);
            Console.WriteLine($"Fast model: {response1}");
            var response2 = await service.AskAsync(question, model: "gpt-4o").ConfigureAwait(false);
            Console.WriteLine($"Capable model: {response2}");
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example7_TemperatureControlAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        var question = "Explain what a variable is in programming.";
        try
        {
            var request1 = new AIChatCompletionRequest
            {
                Messages = new List<AIChatMessage> { new("user", question) },
                Temperature = 0.1
            };
            var response1 = await service.CreateChatCompletionAsync(request1).ConfigureAwait(false);
            Console.WriteLine($"Low temp: {response1.Choices[0].Message.Content}");
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
    }

    public static async Task Example8_ErrorHandlingAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<AudiobookService>();
        try
        {
            await service.AskAsync("").ConfigureAwait(false);
        }
        catch (ArgumentException ex) { Console.WriteLine($"Caught: {ex.GetType().Name}: {ex.Message}"); }
        catch (Exception ex) { Console.WriteLine($"Unexpected: {ex.GetType().Name}: {ex.Message}"); }
    }
}
