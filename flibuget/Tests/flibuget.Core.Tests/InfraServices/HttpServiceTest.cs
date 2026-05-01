using System.Net;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.InfraServices;
using flibuget.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace flibuget.Core.Tests.InfraServices;

public class HttpServiceTests
{
    private readonly IFixture _fixture =
        new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });

 #region Constructor Tests

    [Fact]
    public void Constructor_WhenClientFactoryIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<HttpService>>();

        // Act
        var act = () => new HttpService(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
      .WithParameterName("clientFactory");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var clientFactory = Substitute.For<IHttpClientFactory>();

     // Act
        var act = () => new HttpService(clientFactory, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GET Request Tests

    [Fact]
    public async Task MakeGetRequestAsync_ReturnsContent_WhenResponseIsSuccessful()
    {
   // Arrange
  var uri = new Uri("https://example.com");
  var expectedContent = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
            Content = new StringContent(expectedContent)
   }));

        var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

  // Act
        var result = await sut.MakeGetRequestAsync(uri);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeGetRequestAsync_ThrowsHttpRequestException_WhenResponseIsNotSuccessful()
    {
  // Arrange
        var uri = new Uri("https://example.com");

        var handler = new TestHttpMessageHandler((_, _) =>
  Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

    var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
        Func<Task> act = async () => await sut.MakeGetRequestAsync(uri);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task MakeGetRequestAsync_RespectsCancellationToken()
    {
        // Arrange
        var uri = new Uri("https://example.com");
        var cts = new CancellationTokenSource();
     cts.Cancel();

        var handler = new TestHttpMessageHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
  return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

    var httpClient = new HttpClient(handler);
    var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

      var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

     // Act
        Func<Task> act = async () => await sut.MakeGetRequestAsync(uri, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region JSON POST Request Tests

    [Fact]
public async Task MakeJsonPostRequestAsync_ReturnsContent_WhenResponseIsSuccessful()
    {
        // Arrange
        var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();
        var expectedContent = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
  req.Headers.Accept.Should().Contain(h => h.MediaType == "application/json");
req.Content.Should().NotBeNull();
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
  {
        Content = new StringContent(expectedContent)
        });
        });

    var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
        var result = await sut.MakeJsonPostRequestAsync(uri, data);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeJsonPostRequestAsync_ThrowsArgumentNullException_WhenUriIsNull()
    {
        // Arrange
        var data = _fixture.Create<string>();
  var clientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

      // Act
        Func<Task> act = async () => await sut.MakeJsonPostRequestAsync(null!, data);

     // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MakeJsonPostRequestAsync_ThrowsArgumentException_WhenDataIsNullOrWhitespace(string? data)
  {
        // Arrange
     var uri = new Uri("https://example.com");
        var clientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<HttpService>>();
  var sut = new HttpService(clientFactory, logger);

  // Act
      Func<Task> act = async () => await sut.MakeJsonPostRequestAsync(uri, data!);

   // Assert
        await act.Should().ThrowAsync<ArgumentException>()
          .WithParameterName("data");
    }

    [Fact]
    public async Task MakeJsonPostRequestAsync_ThrowsTimeoutException_WhenRequestTimesOut()
    {
        // Arrange
        var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler(async (_, ct) =>
        {
   await Task.Delay(TimeSpan.FromSeconds(2), ct);
      return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
        Func<Task> act = async () => await sut.MakeJsonPostRequestAsync(uri, data, timeout: 1);

    // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task MakeJsonPostRequestWithBearerAsync_SetsBearerTokenInHeader()
    {
    // Arrange
        var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();
        var bearerToken = "test-token-123";
        var expectedContent = _fixture.Create<string>();

  var handler = new TestHttpMessageHandler((req, _) =>
     {
req.Headers.Authorization.Should().NotBeNull();
            req.Headers.Authorization!.Scheme.Should().Be("Bearer");
            req.Headers.Authorization.Parameter.Should().Be(bearerToken);
       return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
 {
                Content = new StringContent(expectedContent)
            });
   });

   var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

  var logger = Substitute.For<ILogger<HttpService>>();
      var sut = new HttpService(clientFactory, logger);

// Act
    var result = await sut.MakeJsonPostRequestWithBearerAsync(uri, data, bearerToken);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeJsonPostRequestWithApiKeyAsync_SetsApiKeyInHeader()
    {
        // Arrange
   var uri = new Uri("https://example.com");
   var data = _fixture.Create<string>();
        var apiKey = "api-key-123";
      var expectedContent = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
      req.Headers.Should().Contain(h => h.Key == "X-API-Key");
  req.Headers.GetValues("X-API-Key").Should().Contain(apiKey);
       return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
       {
       Content = new StringContent(expectedContent)
    });
        });

        var httpClient = new HttpClient(handler);
   var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

      // Act
     var result = await sut.MakeJsonPostRequestWithApiKeyAsync(uri, data, apiKey);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeJsonPostRequestWithInternalAsync_SetsInternalTokenInHeader()
    {
        // Arrange
     var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();
    var internalKey = "internal-key-123";
        var expectedContent = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
     req.Headers.Authorization.Should().NotBeNull();
         req.Headers.Authorization!.Scheme.Should().Be("internal");
         req.Headers.Authorization.Parameter.Should().Be(internalKey);
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
         Content = new StringContent(expectedContent)
   });
        });

    var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
 var result = await sut.MakeJsonPostRequestWithInternalAsync(uri, data, internalKey);

 // Assert
        result.Should().Be(expectedContent);
    }

    #endregion

    #region XML POST Request Tests

    [Fact]
    public async Task MakeXmlPostRequestAsync_ReturnsContent_WhenResponseIsSuccessful()
    {
   // Arrange
        var uri = new Uri("https://example.com");
var data = "<xml>test</xml>";
        var expectedContent = "<xml>response</xml>";

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            req.Headers.Accept.Should().Contain(h => h.MediaType == "application/xml");
   req.Content.Should().NotBeNull();
  return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
       Content = new StringContent(expectedContent)
            });
        });

        var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
     clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
        var result = await sut.MakeXmlPostRequestAsync(uri, data);

  // Assert
     result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeXmlPostRequestWithBearerAsync_SetsBearerTokenInHeader()
    {
        // Arrange
     var uri = new Uri("https://example.com");
        var data = "<xml>test</xml>";
     var bearerToken = "test-token-123";
        var expectedContent = "<xml>response</xml>";

        var handler = new TestHttpMessageHandler((req, _) =>
        {
       req.Headers.Authorization.Should().NotBeNull();
            req.Headers.Authorization!.Scheme.Should().Be("Bearer");
     req.Headers.Authorization.Parameter.Should().Be(bearerToken);
 req.Headers.Accept.Should().Contain(h => h.MediaType == "application/xml");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
     Content = new StringContent(expectedContent)
   });
        });

        var httpClient = new HttpClient(handler);
  var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
      var sut = new HttpService(clientFactory, logger);

  // Act
   var result = await sut.MakeXmlPostRequestWithBearerAsync(uri, data, bearerToken);

 // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task MakeXmlPostRequestWithApiKeyAsync_SetsApiKeyInHeader()
    {
  // Arrange
        var uri = new Uri("https://example.com");
        var data = "<xml>test</xml>";
        var apiKey = "api-key-123";
        var expectedContent = "<xml>response</xml>";

        var handler = new TestHttpMessageHandler((req, _) =>
  {
          req.Headers.Should().Contain(h => h.Key == "X-API-Key");
         req.Headers.GetValues("X-API-Key").Should().Contain(apiKey);
        req.Headers.Accept.Should().Contain(h => h.MediaType == "application/xml");
   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
     Content = new StringContent(expectedContent)
            });
 });

        var httpClient = new HttpClient(handler);
      var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

  var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
      var result = await sut.MakeXmlPostRequestWithApiKeyAsync(uri, data, apiKey);

  // Assert
      result.Should().Be(expectedContent);
    }

    #endregion

    #region Error Handling Tests

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task MakeJsonPostRequestAsync_ThrowsHttpRequestException_WhenResponseIsNotSuccessful(HttpStatusCode statusCode)
    {
        // Arrange
        var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();

        var handler = new TestHttpMessageHandler((_, _) =>
     Task.FromResult(new HttpResponseMessage(statusCode)));

        var httpClient = new HttpClient(handler);
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(httpClient);

        var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
        Func<Task> act = async () => await sut.MakeJsonPostRequestAsync(uri, data);

        // Assert
 await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task MakeJsonPostRequestAsync_RespectsCancellationToken()
    {
        // Arrange
 var uri = new Uri("https://example.com");
        var data = _fixture.Create<string>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new TestHttpMessageHandler((_, ct) =>
   {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var httpClient = new HttpClient(handler);
  var clientFactory = Substitute.For<IHttpClientFactory>();
     clientFactory.CreateClient().Returns(httpClient);

    var logger = Substitute.For<ILogger<HttpService>>();
        var sut = new HttpService(clientFactory, logger);

        // Act
 Func<Task> act = async () => await sut.MakeJsonPostRequestAsync(uri, data, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Helper Methods

    // Custom HttpMessageHandler to allow mocking SendAsync
    public class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => sendAsync(request, cancellationToken);
    }

    #endregion
}