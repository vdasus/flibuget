using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;
using flibuget.Core.Domain.DTO;
using Xunit;

namespace flibuget.Core.Tests.DomainServices;

public class AudiobookServiceTest
{
    private readonly IFixture _fixture;
  private readonly IAIProvider _aiProvider;
  private readonly ILogger<AudiobookService> _logger;
    private readonly AudiobookService _sut;

    public AudiobookServiceTest()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _aiProvider = _fixture.Freeze<IAIProvider>();
  _logger = _fixture.Freeze<ILogger<AudiobookService>>();
        _sut = new AudiobookService(_aiProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
 {
        // Arrange & Act
        var service = new AudiobookService(_aiProvider, _logger);

    // Assert
  service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullAIProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AudiobookService(null!, _logger);

        // Assert
  act.Should().Throw<ArgumentNullException>()
.WithParameterName("aiProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AudiobookService(_aiProvider, null!);

        // Assert
    act.Should().Throw<ArgumentNullException>()
   .WithParameterName("logger");
    }

    #endregion

    #region AskAsync Tests

 [Theory, AutoData]
    public async Task AskAsync_WithValidQuestion_ShouldReturnResponse(string question, string expectedContent)
    {
      // Arrange
     _aiProvider
       .AskAsync(question, null, null, Arg.Any<CancellationToken>())
  .Returns(expectedContent);

// Act
     var result = await _sut.AskAsync(question);

        // Assert
    result.Should().Be(expectedContent);
   await _aiProvider.Received(1).AskAsync(
    question,
    null,
    null,
    Arg.Any<CancellationToken>());
    }

    [Theory]
#pragma warning disable xUnit1012
    [InlineData(null)]
#pragma warning restore xUnit1012
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_WithInvalidMessage_ShouldThrowArgumentException(string invalidMessage)
    {
        // Arrange
        _aiProvider
    .AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
  .ThrowsAsync(new ArgumentException("User message cannot be null or empty", nameof(invalidMessage)));

      // Act
   var act = async () => await _sut.AskAsync(invalidMessage);

        // Assert
    await act.Should().ThrowAsync<ArgumentException>();
 }

    [Theory, AutoData]
    public async Task AskAsync_WithSystemMessage_ShouldPassToProvider(
      string question,
   string systemMessage,
        string expectedContent)
    {
  // Arrange
        _aiProvider
     .AskAsync(question, systemMessage, null, Arg.Any<CancellationToken>())
      .Returns(expectedContent);

 // Act
    var result = await _sut.AskAsync(question, systemMessage);

        // Assert
        result.Should().Be(expectedContent);
        await _aiProvider.Received(1).AskAsync(
  question,
        systemMessage,
null,
    Arg.Any<CancellationToken>());
    }

  [Theory, AutoData]
    public async Task AskAsync_WithCustomModel_ShouldPassToProvider(
        string question,
        string customModel,
      string expectedContent)
    {
 // Arrange
   _aiProvider
.AskAsync(question, null, customModel, Arg.Any<CancellationToken>())
        .Returns(expectedContent);

        // Act
        var result = await _sut.AskAsync(question, model: customModel);

    // Assert
      result.Should().Be(expectedContent);
 await _aiProvider.Received(1).AskAsync(
 question,
  null,
    customModel,
      Arg.Any<CancellationToken>());
  }

    [Theory, AutoData]
  public async Task AskAsync_WithCancellationToken_ShouldPassTokenToProvider(string question, string expectedContent)
    {
    // Arrange
        using var cts = new CancellationTokenSource();
        _aiProvider
    .AskAsync(question, null, null, cts.Token)
            .Returns(expectedContent);

        // Act
        await _sut.AskAsync(question, cancellationToken: cts.Token);

        // Assert
        await _aiProvider.Received(1).AskAsync(
 question,
      null,
      null,
       cts.Token);
    }

    #endregion

    #region CreateChatCompletionAsync Tests

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WithValidRequest_ShouldReturnResponse(
        AIChatCompletionRequest request)
    {
        // Arrange
        var mockResponse = new AIChatCompletionResponse
        {
      Id = "test-id-123",
            Model = "gpt-4o-mini",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Choices =
 [
    new()
                {
   Index = 0,
      Message = new AIChatMessage
          {
       Role = "assistant",
 Content = "Test response"
    },
   FinishReason = "stop"
   }
          ],
      Usage = new AIUsage
            {
    PromptTokens = 10,
                CompletionTokens = 20,
         TotalTokens = 30
            }
      };

        _aiProvider
            .CreateChatCompletionAsync(request, Arg.Any<CancellationToken>())
          .Returns(mockResponse);

  // Act
      var result = await _sut.CreateChatCompletionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(mockResponse.Id);
        result.Model.Should().Be(mockResponse.Model);
      result.Choices.Should().HaveCount(1);
        result.Usage.Should().NotBeNull();
        result.Usage!.TotalTokens.Should().Be(30);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
     // Arrange & Act
   var act = async () => await _sut.CreateChatCompletionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WhenProviderThrows_ShouldPropagateException(
     AIChatCompletionRequest request)
    {
        // Arrange
     _aiProvider
 .CreateChatCompletionAsync(request, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

 // Act
        var act = async () => await _sut.CreateChatCompletionAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
    .WithMessage("Network error");
    }

    #endregion
}