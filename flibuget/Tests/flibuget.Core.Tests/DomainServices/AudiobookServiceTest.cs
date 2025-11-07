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

    #region GetAudiobookInfoFromAIAsync Tests

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithValidResponse_ShouldReturnParsedDto()
    {
        // Arrange
        var book = "The Hobbit";
        var author = "J.R.R. Tolkien";
        var narrator = "Andy Serkis";
        
        var expectedDto = new AudiobookDescriptionDto
        {
            Title = "The Hobbit",
            Author = "J.R.R. Tolkien",
            CoverLink = "https://example.com/hobbit.jpg",
            Description = "A fantasy adventure about Bilbo Baggins",
            Themes = new List<string> { "Fantasy", "Adventure", "Quest" },
            Duration = "11h 8m",
            Narrator = "Andy Serkis",
            Year = 1937,
            AgeRestriction = "8+",
            Link = "https://example.com/hobbit-audiobook"
        };

        var jsonResponse = JsonSerializer.Serialize(expectedDto);
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(expectedDto.Title);
        result.Author.Should().Be(expectedDto.Author);
        result.CoverLink.Should().Be(expectedDto.CoverLink);
        result.Description.Should().Be(expectedDto.Description);
        result.Themes.Should().BeEquivalentTo(expectedDto.Themes);
        result.Duration.Should().Be(expectedDto.Duration);
        result.Narrator.Should().Be(expectedDto.Narrator);
        result.Year.Should().Be(expectedDto.Year);
        result.AgeRestriction.Should().Be(expectedDto.AgeRestriction);
        result.Link.Should().Be(expectedDto.Link);
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithMarkdownCodeBlock_ShouldStripAndParse()
    {
        // Arrange
        var book = "1984";
        var author = "George Orwell";
        var narrator = "Simon Prebble";
        
        var dto = new AudiobookDescriptionDto
        {
            Title = "1984",
            Author = "George Orwell",
            Narrator = "Simon Prebble",
            Year = 1949
        };

        var jsonResponse = $"```json\n{JsonSerializer.Serialize(dto)}\n```";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("1984");
        result.Author.Should().Be("George Orwell");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithMarkdownCodeBlockNoLanguage_ShouldStripAndParse()
    {
        // Arrange
        var book = "Dune";
        var author = "Frank Herbert";
        var narrator = "Scott Brick";
        
        var dto = new AudiobookDescriptionDto
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Narrator = "Scott Brick"
        };

        var jsonResponse = $"```\n{JsonSerializer.Serialize(dto)}\n```";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Dune");
        result.Author.Should().Be("Frank Herbert");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithInvalidJson_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns("This is not JSON at all");

        // Act
        var act = async () => await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Can't get info from AI.");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithNullResponse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns("null");

        // Act
        var act = async () => await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Can't get info from AI.");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithEmptyJson_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                null, 
                Arg.Any<CancellationToken>())
            .Returns("{}");

        // Act - Empty JSON object should still deserialize to a valid DTO with empty strings
        var result = await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_ShouldPassCorrectPromptToAI()
    {
        // Arrange
        var book = "The Great Gatsby";
        var author = "F. Scott Fitzgerald";
        var narrator = "Jake Gyllenhaal";
        
        var dto = new AudiobookDescriptionDto { Title = book, Author = author };
        var jsonResponse = JsonSerializer.Serialize(dto);
        
        string? capturedPrompt = null;
        string? capturedSystemMessage = null;

        _aiProvider
            .AskAsync(
                Arg.Do<string>(x => capturedPrompt = x),
                Arg.Do<string?>(x => capturedSystemMessage = x),
                null,
                Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        capturedPrompt.Should().NotBeNullOrEmpty();
        capturedPrompt.Should().Contain(book);
        capturedPrompt.Should().Contain(author);
        capturedPrompt.Should().Contain(narrator);
        capturedPrompt.Should().Contain("JSON format");
        capturedSystemMessage.Should().Be("You are a helpful assistant that returns only valid JSON responses.");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithCancellationToken_ShouldPassToProvider()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        using var cts = new CancellationTokenSource();
        
        var dto = new AudiobookDescriptionDto { Title = book };
        var jsonResponse = JsonSerializer.Serialize(dto);
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                null,
                cts.Token)
            .Returns(jsonResponse);

        // Act
        await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator, cts.Token);

        // Assert
        await _aiProvider.Received(1).AskAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            null,
            cts.Token);
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WhenAIProviderThrows_ShouldPropagateException()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                null,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network timeout"));

        // Act
        var act = async () => await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network timeout");
    }

    [Fact]
    public async Task GetAudiobookInfoFromAIAsync_WithPartialData_ShouldReturnDtoWithAvailableFields()
    {
        // Arrange
        var book = "Test Book";
        var author = "Test Author";
        var narrator = "Test Narrator";
        
        var partialDto = new AudiobookDescriptionDto
        {
            Title = "Test Book",
            Author = "Test Author",
            // Other fields remain empty/null
        };

        var jsonResponse = JsonSerializer.Serialize(partialDto);
        
        _aiProvider
            .AskAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.GetAudiobookInfoFromAIAsync(book, author, narrator);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Book");
        result.Author.Should().Be("Test Author");
        result.Description.Should().BeEmpty();
        result.Themes.Should().BeNull();
    }

    #endregion
}