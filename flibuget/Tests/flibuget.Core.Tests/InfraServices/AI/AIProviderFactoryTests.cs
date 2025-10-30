using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace flibuget.Core.Tests.InfraServices.AI;

public class AIProviderFactoryTests
{
    private readonly IFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIProviderFactory> _logger;
 private readonly Dictionary<string, AIProviderConfig> _providerConfigs;

    public AIProviderFactoryTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = Substitute.For<ILogger<AIProviderFactory>>();
  
        _providerConfigs = new Dictionary<string, AIProviderConfig>
        {
 ["OpenAI"] = new AIProviderConfig
         {
       ProviderName = "OpenAI",
            ApiKey = "test-openai-key",
 DefaultModel = "gpt-4o-mini"
      },
            ["Perplexity"] = new AIProviderConfig
        {
ProviderName = "Perplexity",
      ApiKey = "test-perplexity-key",
            DefaultModel = "sonar-pro"
  }
     };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenServiceProviderIsNull_ShouldThrowArgumentNullException()
    {
// Act
  var act = () => new AIProviderFactory(null!, _logger, _providerConfigs);

        // Assert
   act.Should().Throw<ArgumentNullException>()
  .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act
   var act = () => new AIProviderFactory(_serviceProvider, null!, _providerConfigs);

        // Assert
  act.Should().Throw<ArgumentNullException>()
          .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WhenProviderConfigsIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AIProviderFactory(_serviceProvider, _logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
       .WithParameterName("providerConfigs");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Assert
        factory.Should().NotBeNull();
        factory.Should().BeAssignableTo<IAIProviderFactory>();
    }

    [Fact]
    public void Constructor_WithCustomDefaultProvider_ShouldAcceptIt()
    {
        // Act
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs, "Perplexity");

        // Assert
    factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyProviderConfigs_ShouldInitializeSuccessfully()
    {
        // Arrange
   var emptyConfigs = new Dictionary<string, AIProviderConfig>();

        // Act
        var factory = new AIProviderFactory(_serviceProvider, _logger, emptyConfigs);

        // Assert
factory.Should().NotBeNull();
}

    #endregion

    #region CreateProvider Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
 [InlineData("\n")]
    public void CreateProvider_WhenProviderNameIsNullOrWhitespace_ShouldThrowArgumentException(string? providerName)
    {
   // Arrange
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

   // Act
        var act = () => factory.CreateProvider(providerName!);

        // Assert
        act.Should().Throw<ArgumentException>()
         .WithMessage("*Provider name*")
            .WithParameterName("providerName");
 }

    [Fact]
    public void CreateProvider_WhenProviderNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

 // Act
        var act = () => factory.CreateProvider("UnknownProvider");

        // Assert
     act.Should().Throw<InvalidOperationException>()
     .WithMessage("*Provider 'UnknownProvider' is not configured*");
    }

    [Theory]
    [InlineData("Azure")]
    [InlineData("Anthropic")]
    [InlineData("Gemini")]
    public void CreateProvider_WhenProviderIsNotSupported_ShouldThrowNotSupportedException(string unsupportedProvider)
    {
      // Arrange
        var configs = new Dictionary<string, AIProviderConfig>
{
[unsupportedProvider] = new AIProviderConfig
            {
     ProviderName = unsupportedProvider,
            ApiKey = "test-key"
  }
        };
        var factory = new AIProviderFactory(_serviceProvider, _logger, configs);

        // Act
 var act = () => factory.CreateProvider(unsupportedProvider);

     // Assert
      act.Should().Throw<NotSupportedException>()
            .WithMessage($"*Provider '{unsupportedProvider}' is not supported*");
    }

    [Fact]
  public void CreateProvider_ForOpenAI_WhenLoggerServiceNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
_serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns((object?)null);
  var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

    // Act
        var act = () => factory.CreateProvider("OpenAI");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ILogger*");
    }

    [Fact]
    public void CreateProvider_ForOpenAI_WithValidConfiguration_ShouldReturnOpenAIProvider()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
      _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

      // Act
     var provider = factory.CreateProvider("OpenAI");

    // Assert
      provider.Should().NotBeNull();
        provider.Should().BeOfType<OpenAIProvider>();
    provider.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
 public void CreateProvider_ForOpenAI_ShouldLogProviderCreation()
  {
    // Arrange
var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
     var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

     // Act
     var provider = factory.CreateProvider("OpenAI");

        // Assert
 _logger.Received(1).Log(
     LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Creating AI provider: OpenAI")),
            null,
         Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("OPENAI")]
    [InlineData("OpenAI")]
    [InlineData("OpEnAi")]
    [InlineData("oPeNaI")]
    public void CreateProvider_ForOpenAI_ShouldBeCaseInsensitive(string providerName)
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
     _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        
        var configs = new Dictionary<string, AIProviderConfig>
        {
 [providerName] = new AIProviderConfig
      {
                ProviderName = providerName,
 ApiKey = "test-key",
 DefaultModel = "gpt-4o-mini"
 }
        };
        var factory = new AIProviderFactory(_serviceProvider, _logger, configs);

        // Act
        var provider = factory.CreateProvider(providerName);

    // Assert
        provider.Should().NotBeNull();
      provider.Should().BeOfType<OpenAIProvider>();
    }

    [Fact]
    public void CreateProvider_ForPerplexity_WhenWebServiceNotRegistered_ShouldThrowInvalidOperationException()
    {
// Arrange
        _serviceProvider.GetService(typeof(IWebService)).Returns((object?)null);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
    var act = () => factory.CreateProvider("Perplexity");

   // Assert
     act.Should().Throw<InvalidOperationException>()
 .WithMessage("*IWebService*");
    }

    [Fact]
    public void CreateProvider_ForPerplexity_WhenLoggerServiceNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
var webService = Substitute.For<IWebService>();
        _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
  _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns((object?)null);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var act = () => factory.CreateProvider("Perplexity");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ILogger*");
    }

    [Fact]
 public void CreateProvider_ForPerplexity_WithValidConfiguration_ShouldReturnPerplexityProvider()
 {
        // Arrange
 var webService = Substitute.For<IWebService>();
        var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
    _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
  _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
      var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider = factory.CreateProvider("Perplexity");

 // Assert
  provider.Should().NotBeNull();
        provider.Should().BeOfType<PerplexityProvider>();
provider.ProviderName.Should().Be("Perplexity");
    }

    [Fact]
    public void CreateProvider_ForPerplexity_ShouldLogProviderCreation()
    {
        // Arrange
        var webService = Substitute.For<IWebService>();
        var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
        _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
        _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

  // Act
        var provider = factory.CreateProvider("Perplexity");

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
      Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Creating AI provider: Perplexity")),
          null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("perplexity")]
    [InlineData("PERPLEXITY")]
  [InlineData("Perplexity")]
    [InlineData("PeRpLeXiTy")]
    [InlineData("pErPlExItY")]
    public void CreateProvider_ForPerplexity_ShouldBeCaseInsensitive(string providerName)
    {
      // Arrange
   var webService = Substitute.For<IWebService>();
   var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
        _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
   _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
   
      var configs = new Dictionary<string, AIProviderConfig>
        {
          [providerName] = new AIProviderConfig
         {
  ProviderName = providerName,
   ApiKey = "test-key",
        DefaultModel = "sonar-pro"
            }
        };
        var factory = new AIProviderFactory(_serviceProvider, _logger, configs);

      // Act
        var provider = factory.CreateProvider(providerName);

      // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<PerplexityProvider>();
    }

    [Fact]
    public void CreateProvider_CalledMultipleTimes_ShouldCreateNewInstancesEachTime()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
     var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider1 = factory.CreateProvider("OpenAI");
      var provider2 = factory.CreateProvider("OpenAI");

        // Assert
        provider1.Should().NotBeSameAs(provider2);
 provider1.Should().BeOfType<OpenAIProvider>();
   provider2.Should().BeOfType<OpenAIProvider>();
    }

    #endregion

    #region GetDefaultProvider Tests

    [Fact]
    public void GetDefaultProvider_ShouldReturnOpenAIProviderByDefault()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
      var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider = factory.GetDefaultProvider();

        // Assert
  provider.Should().NotBeNull();
        provider.Should().BeOfType<OpenAIProvider>();
     provider.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public void GetDefaultProvider_WithCustomDefaultProvider_ShouldReturnSpecifiedProvider()
    {
        // Arrange
        var webService = Substitute.For<IWebService>();
        var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
        _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
        _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
   var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs, "Perplexity");

        // Act
  var provider = factory.GetDefaultProvider();

 // Assert
        provider.Should().NotBeNull();
    provider.Should().BeOfType<PerplexityProvider>();
        provider.ProviderName.Should().Be("Perplexity");
    }

    [Fact]
    public void GetDefaultProvider_WhenDefaultProviderNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs, "NonExistent");

        // Act
        var act = () => factory.GetDefaultProvider();

        // Assert
        act.Should().Throw<InvalidOperationException>()
          .WithMessage("*Provider 'NonExistent' is not configured*");
    }

    [Fact]
    public void GetDefaultProvider_ShouldLogProviderCreation()
    {
 // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
   var provider = factory.GetDefaultProvider();

      // Assert
        _logger.Received(1).Log(
    LogLevel.Information,
            Arg.Any<EventId>(),
          Arg.Is<object>(o => o.ToString()!.Contains("Creating AI provider:")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void GetDefaultProvider_CalledMultipleTimes_ShouldCreateNewInstancesEachTime()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
   _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
   var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider1 = factory.GetDefaultProvider();
        var provider2 = factory.GetDefaultProvider();

        // Assert
  provider1.Should().NotBeSameAs(provider2);
   provider1.ProviderName.Should().Be(provider2.ProviderName);
    }

    #endregion

    #region Multiple Provider Tests

    [Fact]
    public void CreateProvider_ShouldCreateMultipleProviderInstancesIndependently()
    {
 // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
  var webService = Substitute.For<IWebService>();
        var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
        
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
_serviceProvider.GetService(typeof(IWebService)).Returns(webService);
        _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
   
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

      // Act
 var openAiProvider = factory.CreateProvider("OpenAI");
        var perplexityProvider = factory.CreateProvider("Perplexity");

    // Assert
  openAiProvider.Should().NotBeNull();
  openAiProvider.Should().BeOfType<OpenAIProvider>();
        perplexityProvider.Should().NotBeNull();
        perplexityProvider.Should().BeOfType<PerplexityProvider>();
     openAiProvider.Should().NotBeSameAs(perplexityProvider);
    }

    [Fact]
    public void CreateProvider_WithMultipleConfigurations_ShouldRequestCorrectServices()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
      var webService = Substitute.For<IWebService>();
   var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
        
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
      _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
        _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
      
 var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var openAiProvider = factory.CreateProvider("OpenAI");
        var perplexityProvider = factory.CreateProvider("Perplexity");

        // Assert
        _serviceProvider.Received(1).GetService(typeof(ILogger<OpenAIProvider>));
    _serviceProvider.Received(1).GetService(typeof(IWebService));
        _serviceProvider.Received(1).GetService(typeof(ILogger<PerplexityProvider>));
    }

    #endregion

    #region Service Provider Interaction Tests

    [Fact]
    public void CreateProvider_ForOpenAI_ShouldRequestOnlyRequiredServices()
    {
        // Arrange
     var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

     // Act
        var provider = factory.CreateProvider("OpenAI");

        // Assert
        _serviceProvider.Received(1).GetService(typeof(ILogger<OpenAIProvider>));
        _serviceProvider.DidNotReceive().GetService(typeof(IWebService));
}

    [Fact]
    public void CreateProvider_ForPerplexity_ShouldRequestAllRequiredServices()
    {
        // Arrange
    var webService = Substitute.For<IWebService>();
        var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
    _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
        _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
      var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider = factory.CreateProvider("Perplexity");

        // Assert
        _serviceProvider.Received(1).GetService(typeof(IWebService));
        _serviceProvider.Received(1).GetService(typeof(ILogger<PerplexityProvider>));
    }

    #endregion

    #region Configuration Edge Cases Tests

    [Fact]
    public void CreateProvider_WithConfigurationContainingBaseUrl_ShouldCreateProviderSuccessfully()
    {
        // Arrange
   var configs = new Dictionary<string, AIProviderConfig>
     {
 ["Perplexity"] = new AIProviderConfig
    {
             ProviderName = "Perplexity",
       ApiKey = "test-key",
     BaseUrl = "https://custom.api.url",
  DefaultModel = "sonar-pro"
        }
        };
        
        var webService = Substitute.For<IWebService>();
      var perplexityLogger = Substitute.For<ILogger<PerplexityProvider>>();
  _serviceProvider.GetService(typeof(IWebService)).Returns(webService);
      _serviceProvider.GetService(typeof(ILogger<PerplexityProvider>)).Returns(perplexityLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, configs);

        // Act
     var provider = factory.CreateProvider("Perplexity");

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<PerplexityProvider>();
    }

    [Fact]
    public void CreateProvider_WithConfigurationContainingTimeout_ShouldCreateProviderSuccessfully()
    {
        // Arrange
        var configs = new Dictionary<string, AIProviderConfig>
        {
     ["OpenAI"] = new AIProviderConfig
         {
ProviderName = "OpenAI",
                ApiKey = "test-key",
           DefaultModel = "gpt-4o-mini",
     TimeoutSeconds = 120
        }
        };
        
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, configs);

        // Act
   var provider = factory.CreateProvider("OpenAI");

   // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<OpenAIProvider>();
    }

    #endregion

 #region Interface Implementation Tests

    [Fact]
    public void AIProviderFactory_ShouldImplementIAIProviderFactory()
    {
  // Arrange & Act
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

      // Assert
     factory.Should().BeAssignableTo<IAIProviderFactory>();
    }

    [Fact]
    public void CreateProvider_ShouldReturnIAIProvider()
    {
   // Arrange
      var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
        var provider = factory.CreateProvider("OpenAI");

        // Assert
        provider.Should().BeAssignableTo<IAIProvider>();
    }

    [Fact]
    public void GetDefaultProvider_ShouldReturnIAIProvider()
    {
        // Arrange
        var openAiLogger = Substitute.For<ILogger<OpenAIProvider>>();
        _serviceProvider.GetService(typeof(ILogger<OpenAIProvider>)).Returns(openAiLogger);
        var factory = new AIProviderFactory(_serviceProvider, _logger, _providerConfigs);

        // Act
    var provider = factory.GetDefaultProvider();

        // Assert
        provider.Should().BeAssignableTo<IAIProvider>();
    }

    #endregion
}
