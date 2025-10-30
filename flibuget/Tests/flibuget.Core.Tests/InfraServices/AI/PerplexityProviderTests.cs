using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.Domain.DTO;
using flibuget.Core.InfraServices; // for IWebService
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;
// ReSharper disable ObjectCreationAsStatement
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

namespace flibuget.Core.Tests.InfraServices.AI;

public class PerplexityProviderTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
    private readonly AIProviderConfig _validConfig = new()
    {
        ProviderName = "Perplexity",
        ApiKey = "test-api-key",
        BaseUrl = "https://api.perplexity.ai",
        DefaultModel = "sonar-pro"
    };

    private static PerplexityChatCompletionResponse CreateSampleResponse(string content = "Hello world") => new()
    {
        Id = "chatcmpl-123",
        Object = "chat.completion",
        Created = 1730290000,
        Model = "sonar-pro",
        Choices =
        [
            new PerplexityChoice
            {
                Index = 0,
                FinishReason = "stop",
                Message = new PerplexityResponseMessage { Role = "assistant", Content = content }
            }
        ],
        Usage = new PerplexityUsage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
    };

    #region Constructor
    [Fact]
    public void Constructor_WhenConfigNull_ShouldThrow()
    {
        var web = Substitute.For<IWebService>();
        var logger = Substitute.For<ILogger<PerplexityProvider>>();
        Action act = () => new PerplexityProvider(null!, web, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_WhenWebServiceNull_ShouldThrow()
    {
        var logger = Substitute.For<ILogger<PerplexityProvider>>();
        Action act = () => new PerplexityProvider(_validConfig, null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("webService");
    }

    [Fact]
    public void Constructor_WhenLoggerNull_ShouldThrow()
    {
        var web = Substitute.For<IWebService>();
        Action act = () => new PerplexityProvider(_validConfig, web, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenApiKeyInvalid_ShouldThrow(string? apiKey)
    {
        var web = Substitute.For<IWebService>();
        var logger = Substitute.For<ILogger<PerplexityProvider>>();
        var cfg = new AIProviderConfig { ProviderName = "Perplexity", ApiKey = apiKey! };
        Action act = () => new PerplexityProvider(cfg, web, logger);
        act.Should().Throw<ArgumentException>().WithMessage("*API key*");
    }

    [Fact]
    public void Constructor_Valid_ShouldLogInitialization()
    {
        var web = Substitute.For<IWebService>();
        var testLogger = new TestLogger<PerplexityProvider>();
        var provider = new PerplexityProvider(_validConfig, web, testLogger);
        testLogger.Logs.Should().Contain(l => l.Contains("Perplexity provider initialized") && l.Contains("sonar-pro"));
        provider.ProviderName.Should().Be("Perplexity");
    }
    #endregion

    #region CreateChatCompletionAsync validation
    [Fact]
    public async Task CreateChatCompletionAsync_WhenRequestNull_ShouldThrow()
    {
        var provider = new PerplexityProvider(_validConfig, Substitute.For<IWebService>(), Substitute.For<ILogger<PerplexityProvider>>());
        Func<Task> act = () => provider.CreateChatCompletionAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenMessagesNull_ShouldThrow()
    {
        var provider = new PerplexityProvider(_validConfig, Substitute.For<IWebService>(), Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest { Messages = null! };
        Func<Task> act = () => provider.CreateChatCompletionAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Messages*");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenMessagesEmpty_ShouldThrow()
    {
        var provider = new PerplexityProvider(_validConfig, Substitute.For<IWebService>(), Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest { Messages = new List<AIChatMessage>() };
        Func<Task> act = () => provider.CreateChatCompletionAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Messages*");
    }
    #endregion

    #region CreateChatCompletionAsync success mapping
    [Fact]
    public async Task CreateChatCompletionAsync_WhenValid_ShouldMapResponse()
    {
        var web = Substitute.For<IWebService>();
        var responseObj = CreateSampleResponse();
        var json = JsonSerializer.Serialize(responseObj);
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(json));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")]
        };
        var result = await provider.CreateChatCompletionAsync(request);
        result.Id.Should().Be(responseObj.Id);
        result.Object.Should().Be(responseObj.Object);
        result.Model.Should().Be(responseObj.Model);
        result.Choices.Should().HaveCount(1);
        result.Choices[0].Message.Content.Should().Be("Hello world");
        result.Usage.Should().NotBeNull();
        result.Usage!.PromptTokens.Should().Be(10);
        result.Usage.CompletionTokens.Should().Be(5);
        result.Usage.TotalTokens.Should().Be(15);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenResponseNull_ShouldThrow()
    {
        var web = Substitute.For<IWebService>();
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("null"));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest { Messages = [new("user", "Test")] };
        Func<Task> act = () => provider.CreateChatCompletionAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*deserialize*");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenUsageMissing_ShouldSetUsageNull()
    {
        var web = Substitute.For<IWebService>();
        var responseObj = CreateSampleResponse();
        responseObj.Usage = null; // remove usage
        var json = JsonSerializer.Serialize(responseObj);
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(json);
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest { Messages = [new("user", "Test")] };
        var result = await provider.CreateChatCompletionAsync(request);
        result.Usage.Should().BeNull();
    }
    #endregion

    #region Request serialization
    [Fact]
    public async Task CreateChatCompletionAsync_ShouldSerializeOptions()
    {
        var capturedJson = string.Empty;
        var web = Substitute.For<IWebService>();
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Do<string>(s => capturedJson = s), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(JsonSerializer.Serialize(CreateSampleResponse())));
        var provider = new PerplexityProvider(new AIProviderConfig { ProviderName = "Perplexity", ApiKey = "k", DefaultModel = null }, web, Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")],
            Temperature = 0.55,
            MaxTokens = 999,
            TopP = 0.8,
            Stream = true
        };
        await provider.CreateChatCompletionAsync(request);
        capturedJson.Should().Contain("\"temperature\":0.55");
        capturedJson.Should().Contain("\"max_tokens\":999");
        capturedJson.Should().Contain("\"top_p\":0.8");
        capturedJson.Should().Contain("\"stream\":true");
        capturedJson.Should().Contain("\"model\":\"sonar-pro\""); // fallback default
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithModelOverride_ShouldUseOverride()
    {
        var capturedJson = string.Empty;
        var web = Substitute.For<IWebService>();
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Do<string>(s => capturedJson = s), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(JsonSerializer.Serialize(CreateSampleResponse())));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var request = new AIChatCompletionRequest { Messages = [new("user", "Test")], Model = "custom-model" };
        await provider.CreateChatCompletionAsync(request);
        capturedJson.Should().Contain("\"model\":\"custom-model\"");
    }
    #endregion

    #region AskAsync
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AskAsync_WhenUserMessageInvalid_ShouldThrow(string? msg)
    {
        var provider = new PerplexityProvider(_validConfig, Substitute.For<IWebService>(), Substitute.For<ILogger<PerplexityProvider>>());
        Func<Task> act = () => provider.AskAsync(msg!);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*User message*");
    }

    [Fact]
    public async Task AskAsync_WhenChoicesEmpty_ShouldThrow()
    {
        var web = Substitute.For<IWebService>();
        var emptyResponse = new PerplexityChatCompletionResponse
        {
            Id = "x",
            Object = "chat.completion",
            Created = 1,
            Model = "sonar-pro",
            Choices = new List<PerplexityChoice>()
        };
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(JsonSerializer.Serialize(emptyResponse)));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        Func<Task> act = () => provider.AskAsync("Test question");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No choices*");
    }

    [Fact]
    public async Task AskAsync_WhenValid_ShouldReturnFirstChoiceContent()
    {
        var web = Substitute.For<IWebService>();
        var sample = CreateSampleResponse("Answer content");
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(JsonSerializer.Serialize(sample)));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var answer = await provider.AskAsync("What is2+2?", systemMessage: "You are a math bot.");
        answer.Should().Be("Answer content");
    }

    [Fact]
    public async Task AskAsync_WithModelOverride_ShouldSerializeModel()
    {
        var capturedJson = string.Empty;
        var web = Substitute.For<IWebService>();
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Do<string>(s => capturedJson = s), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(JsonSerializer.Serialize(CreateSampleResponse("42"))));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        _ = await provider.AskAsync("Compute", model: "sonar-medium");
        capturedJson.Should().Contain("\"model\":\"sonar-medium\"");
    }
    #endregion

    #region Cancellation
    [Fact]
    public async Task CreateChatCompletionAsync_WhenCancelled_ShouldThrowOperationCanceled()
    {
        var web = Substitute.For<IWebService>();
        web.MakeJsonPostRequestWithBearerAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(_ => Task.FromException<string>(new OperationCanceledException()));
        var provider = new PerplexityProvider(_validConfig, web, Substitute.For<ILogger<PerplexityProvider>>());
        var req = new AIChatCompletionRequest { Messages = [new("user", "Hi")] };
        Func<Task> act = () => provider.CreateChatCompletionAsync(req, new CancellationToken(true));
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    #endregion

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Logs { get; } = new();
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add(formatter(state, exception));
        }
        private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
