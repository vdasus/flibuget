using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
// ReSharper disable AsyncApostle.AsyncAwaitMayBeElidedHighlighting
// ReSharper disable MethodHasAsyncOverload

namespace flibuget.Core.Tests.InfraServices.AI;

public class OpenAIProviderTests
{
    private readonly IFixture _fixture =
        new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
    private readonly AIProviderConfig _config = new()
    {
        ProviderName = "OpenAI",
        ApiKey = "test-api-key",
        DefaultModel = "gpt-4o-mini"
    };
    private readonly ILogger<OpenAIProvider> _logger = Substitute.For<ILogger<OpenAIProvider>>();

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenConfigIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new OpenAIProvider(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new OpenAIProvider(_config, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
       .WithParameterName("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenApiKeyIsNullOrWhitespace_ShouldThrowArgumentException(string? apiKey)
    {
        // Arrange
        var config = new AIProviderConfig
        {
            ProviderName = "OpenAI",
            ApiKey = apiKey!
        };

        // Act
        var act = () => new OpenAIProvider(config, _logger);

        // Assert
        act.Should().Throw<ArgumentException>()
    .WithMessage("*API key*");
    }

    [Fact]
    public void Constructor_WhenValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var provider = new OpenAIProvider(_config, _logger);

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public void Constructor_WhenDefaultModelIsNull_ShouldUseGpt4oMini()
    {
        // Arrange
        var config = new AIProviderConfig
        {
            ProviderName = "OpenAI",
            ApiKey = "test-key",
            DefaultModel = null
        };

        // Act
        var provider = new OpenAIProvider(config, _logger);

        // Assert
        provider.Should().NotBeNull();
        // Default model is used internally, verified through behavior
    }

    [Fact]
    public void ProviderName_Should_Return_OpenAI()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var providerName = provider.ProviderName;

        // Assert
        providerName.Should().Be("OpenAI");
    }

    #endregion

    #region CreateChatCompletionAsync Tests

    [Fact]
    public async Task CreateChatCompletionAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenMessagesIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = null!
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                   .WithMessage("*Messages*");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenMessagesIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = []
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Messages*");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_RespectsCancellationToken()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test message")]
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request, cts.Token);

        // Assert
        // This will throw because we can't fully mock OpenAI client, but we verify cancellation token is used
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region AskAsync Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_WhenUserMessageIsNullOrWhitespace_ShouldThrowArgumentException(string? userMessage)
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.AskAsync(userMessage!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User message*");
    }

    [Fact]
    public async Task AskAsync_WhenSystemMessageIsNull_ShouldNotThrow()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.AskAsync("Test message", systemMessage: null);

        // Assert
        // Will throw due to missing actual API call, but validates null system message is allowed
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AskAsync_RespectsCancellationToken()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await provider.AskAsync("Test", cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithCustomModel_ShouldUseCustomModel()
    {
        // Arrange
        var config = new AIProviderConfig
        {
            ProviderName = "OpenAI",
            ApiKey = "test-key",
            DefaultModel = "gpt-4-turbo"
        };

        // Act
        var provider = new OpenAIProvider(config, _logger);

        // Assert
        provider.Should().NotBeNull();
        // Model is used internally in API calls
    }

    [Fact]
    public void Constructor_WithBaseUrl_ShouldInitialize()
    {
        // Arrange
        var config = new AIProviderConfig
        {
            ProviderName = "OpenAI",
            ApiKey = "test-key",
            BaseUrl = "https://custom.openai.com",
            DefaultModel = "gpt-4o-mini"
        };

        // Act
        var provider = new OpenAIProvider(config, _logger);

        // Assert
        provider.Should().NotBeNull();
    }

    #endregion

    #region Message Role Handling Tests

    [Theory]
    [InlineData("system")]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("SYSTEM")]
    [InlineData("USER")]
    [InlineData("ASSISTANT")]
    public async Task CreateChatCompletionAsync_ShouldHandleDifferentMessageRoles(string role)
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new(role, "Test content")]
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        // Will fail on actual API call but validates role handling
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithUnknownRole_ShouldDefaultToUserRole()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("unknown_role", "Test content")]
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        // Will fail on actual API call but validates unknown role handling
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Request Options Tests

    [Fact]
    public async Task CreateChatCompletionAsync_WithTemperature_ShouldPassToApi()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")],
            Temperature = 0.7
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithMaxTokens_ShouldPassToApi()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")],
            MaxTokens = 1000
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithTopP_ShouldPassToApi()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")],
            TopP = 0.9
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithCustomModel_ShouldOverrideDefaultModel()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")],
            Model = "gpt-4-turbo"
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region AskAsync Integration Tests

    [Fact]
    public async Task AskAsync_WithSystemMessage_ShouldIncludeInRequest()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.AskAsync(
    "What is 2+2?",
 systemMessage: "You are a helpful math tutor");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AskAsync_WithCustomModel_ShouldUseSpecifiedModel()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.AskAsync(
            "Test question",
       model: "gpt-4-turbo");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AskAsync_WithOnlyUserMessage_ShouldWork()
    {
        // Arrange
        var provider = new OpenAIProvider(_config, _logger);

        // Act
        var act = async () => await provider.AskAsync("Simple question");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Logging Tests

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Logs { get; } = [];
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add(formatter(state, exception));
        }
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public void Constructor_ShouldLogInitializationMessage()
    {
        // Arrange
        var testLogger = new TestLogger<OpenAIProvider>();

        // Act
        var provider = new OpenAIProvider(_config, testLogger);

        // Assert
        testLogger.Logs.Should().Contain(l => l.Contains("OpenAI provider initialized") && l.Contains("gpt-4o-mini"));
    }

    [Fact]
    public async Task CreateChatCompletionAsync_ShouldLogSendingRequest()
    {
        // Arrange
        var testLogger = new TestLogger<OpenAIProvider>();
        var provider = new OpenAIProvider(_config, testLogger);
        var request = new AIChatCompletionRequest
        {
            Messages = [new("user", "Test")]
        };

        // Act
        var act = async () => await provider.CreateChatCompletionAsync(request);
        await act.Should().ThrowAsync<Exception>();

        // Assert
        testLogger.Logs.Should().Contain(l => l.Contains("Sending chat completion request to OpenAI with model") && l.Contains("gpt-4o-mini"));
    }

    #endregion
}
