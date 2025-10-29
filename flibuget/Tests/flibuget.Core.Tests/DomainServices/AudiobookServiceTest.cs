using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices;
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
    private readonly IWebService _webService;
    private readonly ILogger<AudiobookService> _logger;
    private readonly string _apiKey;
    private readonly AudiobookService _sut;

    public AudiobookServiceTest()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _webService = _fixture.Freeze<IWebService>();
        _logger = _fixture.Freeze<ILogger<AudiobookService>>();
        _apiKey = "pplx-test-api-key-12345";
        _sut = new AudiobookService(_webService, _logger, _apiKey);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new AudiobookService(_webService, _logger, _apiKey);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullWebService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AudiobookService(null!, _logger, _apiKey);

        // Assert
        act.Should().Throw<ArgumentNullException>()
                .WithParameterName("webService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AudiobookService(_webService, null!, _apiKey);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Theory]
#pragma warning disable xUnit1012
    [InlineData(null)]
#pragma warning restore xUnit1012
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidApiKey_ShouldThrowArgumentException(string invalidApiKey)
    {
        // Arrange & Act
        var act = () => new AudiobookService(_webService, _logger, invalidApiKey);

        // Assert
        act.Should().Throw<ArgumentException>()
       .WithParameterName("apiKey")
         .WithMessage("API key cannot be null or empty*");
    }

    #endregion

    #region AskAsync Tests

    [Theory, AutoData]
    public async Task AskAsync_WithValidQuestion_ShouldReturnResponse(string question, string expectedContent)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Id = "test-id",
            Choices = new List<PerplexityChoice>
            {
       new() { Message = new PerplexityResponseMessage { Content = expectedContent } }
            }
        };

        _webService
      .MakeJsonPostRequestWithBearerAsync(
           Arg.Any<Uri>(),
       Arg.Any<string>(),
        _apiKey,
     240,
         Arg.Any<CancellationToken>())
          .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _sut.AskAsync(question);

        // Assert
        result.Should().Be(expectedContent);
        await _webService.Received(1).MakeJsonPostRequestWithBearerAsync(
          Arg.Is<Uri>(u => u.ToString().Contains("chat/completions")),
            Arg.Any<string>(),
            _apiKey,
         240,
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
        // Arrange & Act
        var act = async () => await _sut.AskAsync(invalidMessage);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("userMessage")
                  .WithMessage("User message cannot be null or empty*");
    }

    [Theory, AutoData]
    public async Task AskAsync_WithCustomModel_ShouldUseSpecifiedModel(string question, string customModel)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
       {
     new() { Message = new PerplexityResponseMessage { Content = "response" } }
            }
        };

        string capturedRequest = string.Empty;
        _webService
         .MakeJsonPostRequestWithBearerAsync(
  Arg.Any<Uri>(),
Arg.Do<string>(req => capturedRequest = req),
          _apiKey,
     240,
         Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.AskAsync(question, model: customModel);

        // Assert
        capturedRequest.Should().Contain(customModel);
    }

    [Theory, AutoData]
    public async Task AskAsync_WhenNoChoicesReturned_ShouldThrowInvalidOperationException(string question)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>()
        };

        _webService
               .MakeJsonPostRequestWithBearerAsync(
    Arg.Any<Uri>(),
        Arg.Any<string>(),
       _apiKey,
                 240,
     Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        var act = async () => await _sut.AskAsync(question);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
              .WithMessage("No choices returned from Perplexity API");
    }

    [Theory, AutoData]
    public async Task AskAsync_WithCancellationToken_ShouldPassTokenToWebService(string question)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
     {
    new() { Message = new PerplexityResponseMessage { Content = "response" } }
          }
        };

        _webService
            .MakeJsonPostRequestWithBearerAsync(
     Arg.Any<Uri>(),
     Arg.Any<string>(),
                _apiKey,
          240,
     cts.Token)
          .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.AskAsync(question, cancellationToken: cts.Token);

        // Assert
        await _webService.Received(1).MakeJsonPostRequestWithBearerAsync(
        Arg.Any<Uri>(),
      Arg.Any<string>(),
                _apiKey,
      240,
                cts.Token);
    }

    #endregion

    #region CreateChatCompletionAsync Tests

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WithValidMessages_ShouldReturnResponse(
        List<PerplexityMessage> messages)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Id = "test-id-123",
            Model = "sonar-pro",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Choices =
            [
                new()
                {
                    Index = 0,
                    Message = new PerplexityResponseMessage
                    {
                        Role = "assistant",
                        Content = "Test response"
                    },
                    FinishReason = "stop"
                }
            ],
            Usage = new PerplexityUsage
            {
                PromptTokens = 10,
                CompletionTokens = 20,
                TotalTokens = 30
            }
        };

        _webService
            .MakeJsonPostRequestWithBearerAsync(
           Arg.Any<Uri>(),
   Arg.Any<string>(),
          _apiKey,
       240,
        Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _sut.CreateChatCompletionAsync(messages);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(mockResponse.Id);
        result.Model.Should().Be(mockResponse.Model);
        result.Choices.Should().HaveCount(1);
        result.Usage.Should().NotBeNull();
        result.Usage!.TotalTokens.Should().Be(30);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _sut.CreateChatCompletionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithEmptyMessages_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyMessages = new List<PerplexityMessage>();

        // Act
        var act = async () => await _sut.CreateChatCompletionAsync(emptyMessages);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messages")
  .WithMessage("Messages list cannot be empty*");
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WithTemperature_ShouldIncludeInRequest(
        List<PerplexityMessage> messages,
        double temperature)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
            {
     new() { Message = new PerplexityResponseMessage { Content = "response" } }
         }
        };

        string capturedRequest = string.Empty;
        _webService
            .MakeJsonPostRequestWithBearerAsync(
                Arg.Any<Uri>(),
       Arg.Do<string>(req => capturedRequest = req),
                _apiKey,
          240,
     Arg.Any<CancellationToken>())
          .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.CreateChatCompletionAsync(messages, temperature: temperature);

        // Assert
        capturedRequest.Should().Contain("temperature");
        capturedRequest.Should().Contain(temperature.ToString());
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WithMaxTokens_ShouldIncludeInRequest(
     List<PerplexityMessage> messages,
      int maxTokens)
    {
        // Arrange
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
     {
           new() { Message = new PerplexityResponseMessage { Content = "response" } }
        }
        };

        string capturedRequest = string.Empty;
        _webService
            .MakeJsonPostRequestWithBearerAsync(
  Arg.Any<Uri>(),
      Arg.Do<string>(req => capturedRequest = req),
                _apiKey,
        240,
 Arg.Any<CancellationToken>())
     .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.CreateChatCompletionAsync(messages, maxTokens: maxTokens);

        // Assert
        capturedRequest.Should().Contain("max_tokens");
        capturedRequest.Should().Contain(maxTokens.ToString());
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WithResponseFormat_ShouldIncludeInRequest(
        List<PerplexityMessage> messages)
    {
        // Arrange
        var responseFormat = new PerplexityResponseFormat
        {
            Type = "json_schema",
            JsonSchema = new PerplexityJsonSchemaWrapper
            {
                Schema = new PerplexityJsonSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        ["test"] = new Dictionary<string, string> { ["type"] = "string" }
                    }
                }
            }
        };

        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
    {
                new() { Message = new PerplexityResponseMessage { Content = """{"test":"value"}""" } }
    }
        };

        string capturedRequest = string.Empty;
        _webService
        .MakeJsonPostRequestWithBearerAsync(
      Arg.Any<Uri>(),
      Arg.Do<string>(req => capturedRequest = req),
   _apiKey,
     240,
    Arg.Any<CancellationToken>())
   .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.CreateChatCompletionAsync(messages, responseFormat: responseFormat);

        // Assert
        capturedRequest.Should().Contain("response_format");
        capturedRequest.Should().Contain("json_schema");
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WhenApiReturnsInvalidJson_ShouldThrowInvalidOperationException(
        List<PerplexityMessage> messages)
    {
        // Arrange
        _webService
              .MakeJsonPostRequestWithBearerAsync(
           Arg.Any<Uri>(),
                Arg.Any<string>(),
    _apiKey,
        240,
             Arg.Any<CancellationToken>())
              .Returns("invalid json {{{");

        // Act
        var act = async () => await _sut.CreateChatCompletionAsync(messages);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Theory, AutoData]
    public async Task CreateChatCompletionAsync_WhenWebServiceThrows_ShouldPropagateException(
            List<PerplexityMessage> messages)
    {
        // Arrange
        _webService
       .MakeJsonPostRequestWithBearerAsync(
          Arg.Any<Uri>(),
         Arg.Any<string>(),
            _apiKey,
       240,
            Arg.Any<CancellationToken>())
        .Throws(new HttpRequestException("Network error"));

        // Act
        var act = async () => await _sut.CreateChatCompletionAsync(messages);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }

    #endregion

    #region CreateStructuredCompletionAsync Tests

    [Theory, AutoData]
    public async Task CreateStructuredCompletionAsync_WithValidParameters_ShouldReturnStructuredResponse(
 string userMessage)
    {
        // Arrange
        var schema = new PerplexityJsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["companies"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["name"] = new Dictionary<string, string> { ["type"] = "string" }
                        }
                    }
                }
            },
            Required = new List<string> { "companies" }
        };

        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
        {
       new()
      {
     Message = new PerplexityResponseMessage
    {
           Content = """{"companies":[{"name":"TestCorp"}]}"""
  }
    }
     }
        };

        _webService
          .MakeJsonPostRequestWithBearerAsync(
               Arg.Any<Uri>(),
                Arg.Any<string>(),
         _apiKey,
             240,
        Arg.Any<CancellationToken>())
              .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        var result = await _sut.CreateStructuredCompletionAsync(userMessage, schema);

        // Assert
        result.Should().NotBeNull();
        result.Choices.Should().HaveCount(1);
        result.Choices[0].Message.Content.Should().Contain("companies");
    }

    [Theory, AutoData]
    public async Task CreateStructuredCompletionAsync_ShouldIncludeResponseFormatInRequest(
    string userMessage)
    {
        // Arrange
        var schema = new PerplexityJsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["test"] = new Dictionary<string, string> { ["type"] = "string" }
            }
        };

        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
         {
      new() { Message = new PerplexityResponseMessage { Content = """{"test":"value"}""" } }
     }
        };

        string capturedRequest = string.Empty;
        _webService
            .MakeJsonPostRequestWithBearerAsync(
      Arg.Any<Uri>(),
   Arg.Do<string>(req => capturedRequest = req),
        _apiKey,
   240,
  Arg.Any<CancellationToken>())
   .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.CreateStructuredCompletionAsync(userMessage, schema);

        // Assert
        capturedRequest.Should().Contain("response_format");
        capturedRequest.Should().Contain("json_schema");
        capturedRequest.Should().Contain("\"type\":\"json_schema\"");
    }

    [Theory, AutoData]
    public async Task CreateStructuredCompletionAsync_WithCustomModel_ShouldUseSpecifiedModel(
        string userMessage,
    string customModel)
    {
        // Arrange
        var schema = new PerplexityJsonSchema { Type = "object" };
        var mockResponse = new PerplexityChatCompletionResponse
        {
            Choices = new List<PerplexityChoice>
{
   new() { Message = new PerplexityResponseMessage { Content = "{}" } }
  }
        };

        string capturedRequest = string.Empty;
        _webService
     .MakeJsonPostRequestWithBearerAsync(
        Arg.Any<Uri>(),
      Arg.Do<string>(req => capturedRequest = req),
    _apiKey,
        240,
         Arg.Any<CancellationToken>())
   .Returns(JsonSerializer.Serialize(mockResponse));

        // Act
        await _sut.CreateStructuredCompletionAsync(userMessage, schema, model: customModel);

        // Assert
        capturedRequest.Should().Contain(customModel);
    }

    #endregion
}