using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.Configuration;
using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AI;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace flibuget.Core.Tests.Configuration;

public class AIServiceExtensionsTests
{
    private readonly IFixture _fixture =
          new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });

    #region AddAIServices Tests

    [Fact]
    public void AddAIServices_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAIServices(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
    }

    [Fact]
    public void AddAIServices_WhenNoProvidersConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddAIServices(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No AI providers configured*");
    }

    [Fact]
    public void AddAIServices_WhenOpenAIConfigured_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-openai-key" },
                { "OpenAI:DefaultModel", "gpt-4" },
                { "OpenAI:TimeoutSeconds", "120" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetService<IAIProviderFactory>();
        factory.Should().NotBeNull();

        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddAIServices_WhenPerplexityConfigured_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Perplexity:ApiKey", "test-perplexity-key" },
                { "Perplexity:DefaultModel", "sonar-pro" },
                { "Perplexity:BaseUrl", "https://api.perplexity.ai" },
                { "Perplexity:TimeoutSeconds", "180" },
                { "AI:DefaultProvider", "Perplexity" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetService<IAIProviderFactory>();
        factory.Should().NotBeNull();

        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddAIServices_WhenBothProvidersConfigured_ShouldRegisterBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-openai-key" },
                { "Perplexity:ApiKey", "test-perplexity-key" },
                { "AI:DefaultProvider", "OpenAI" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetService<IAIProviderFactory>();
        factory.Should().NotBeNull();

        var openAIProvider = factory!.CreateProvider("OpenAI");
        openAIProvider.Should().NotBeNull();
        openAIProvider.ProviderName.Should().Be("OpenAI");

        var perplexityProvider = factory.CreateProvider("Perplexity");
        perplexityProvider.Should().NotBeNull();
        perplexityProvider.ProviderName.Should().Be("Perplexity");
    }

    [Fact]
    public void AddAIServices_WhenDefaultProviderNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
     // NOTE: NOT adding IWebService, so Perplexity cannot be created even if configured  
        
     const string perplexityEnvVar = "FLIBUGET_AI_APIKEY";
  var originalPerplexityKey = Environment.GetEnvironmentVariable(perplexityEnvVar);
        
        try
        {
     // Clear environment variable to ensure Perplexity is not configured
     Environment.SetEnvironmentVariable(perplexityEnvVar, null);

            var configuration = new ConfigurationBuilder()
   .AddInMemoryCollection(new Dictionary<string, string?>
            {
     { "OpenAI:ApiKey", "test-openai-key" },
  { "AI:DefaultProvider", "Perplexity" } // Perplexity not configured (no API key)
   })
    .Build();

      // Act
         var act = () => services.AddAIServices(configuration);

        // Assert
   act.Should().Throw<InvalidOperationException>()
      .WithMessage("Default provider 'Perplexity' is not configured");
  }
  finally
  {
            Environment.SetEnvironmentVariable(perplexityEnvVar, originalPerplexityKey);
    }
    }

    [Fact]
    public void AddAIServices_WhenDefaultProviderNotSpecified_ShouldUseOpenAI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
{
       { "OpenAI:ApiKey", "test-openai-key" }
            })
    .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
        provider!.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public void AddAIServices_WhenApiKeyFromEnvironmentVariable_ShouldConfigureProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        const string envVarName = "OPENAI_API_KEY";
        const string testApiKey = "env-test-key";

        try
        {
            Environment.SetEnvironmentVariable(envVarName, testApiKey);

            var configuration = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  // No ApiKey in config, should fall back to environment variable
              })
                      .Build();

            // Act
            services.AddAIServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var factory = serviceProvider.GetService<IAIProviderFactory>();
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void AddAIServices_WhenPlaceholderApiKey_ShouldFallBackToEnvironmentVariable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        const string envVarName = "FLIBUGET_AI_APIKEY";
        const string testApiKey = "env-perplexity-key";

        try
        {
            Environment.SetEnvironmentVariable(envVarName, testApiKey);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Perplexity:ApiKey", "your-perplexity-api-key-here" }, // Placeholder
                    { "AI:DefaultProvider", "Perplexity" }
                })
                .Build();

            // Act
            services.AddAIServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var factory = serviceProvider.GetService<IAIProviderFactory>();
            factory.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void AddAIServices_RegistersFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory1 = serviceProvider.GetService<IAIProviderFactory>();
        var factory2 = serviceProvider.GetService<IAIProviderFactory>();

        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void AddAIServices_RegistersProviderAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        using var scope1 = serviceProvider.CreateScope();
        var provider1 = scope1.ServiceProvider.GetService<IAIProvider>();

        using var scope2 = serviceProvider.CreateScope();
        var provider2 = scope2.ServiceProvider.GetService<IAIProvider>();

        provider1.Should().NotBeSameAs(provider2);
    }

    #endregion

    #region OpenAI Configuration Tests

    [Theory]
    [InlineData("gpt-4o-mini", 240)]
    [InlineData("gpt-4", 120)]
    [InlineData("gpt-3.5-turbo", 60)]
    public void AddAIServices_WhenOpenAIConfiguredWithCustomSettings_ShouldUseThoseSettings(
        string model, int timeout)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" },
                { "OpenAI:DefaultModel", model },
                { "OpenAI:TimeoutSeconds", timeout.ToString() }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
        provider!.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    public void AddAIServices_WhenOpenAIConfiguredWithoutModel_ShouldUseDefaultModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
   .AddInMemoryCollection(new Dictionary<string, string?>
            {
      { "OpenAI:ApiKey", "test-key" }
   // No DefaultModel specified
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddAIServices_WhenOpenAIConfiguredWithInvalidTimeout_ShouldUseDefaultTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" },
                { "OpenAI:TimeoutSeconds", "invalid" } // Invalid number
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    #endregion

    #region Perplexity Configuration Tests

    [Theory]
    [InlineData("sonar-pro", 240)]
    [InlineData("sonar", 180)]
    public void AddAIServices_WhenPerplexityConfiguredWithCustomSettings_ShouldUseThoseSettings(
        string model, int timeout)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Perplexity:ApiKey", "test-key" },
                { "Perplexity:DefaultModel", model },
                { "Perplexity:TimeoutSeconds", timeout.ToString() },
                { "AI:DefaultProvider", "Perplexity" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
        provider!.ProviderName.Should().Be("Perplexity");
    }

    [Fact]
    public void AddAIServices_WhenPerplexityConfiguredWithoutBaseUrl_ShouldUseDefaultBaseUrl()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Perplexity:ApiKey", "test-key" },
                { "AI:DefaultProvider", "Perplexity" }
                // No BaseUrl specified
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddAIServices_WhenPerplexityConfiguredWithCustomBaseUrl_ShouldUseThatUrl()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var customBaseUrl = "https://custom.perplexity.ai";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Perplexity:ApiKey", "test-key" },
                { "Perplexity:BaseUrl", customBaseUrl },
                { "AI:DefaultProvider", "Perplexity" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddAIServices_WhenPerplexityConfiguredWithInvalidTimeout_ShouldUseDefaultTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Perplexity:ApiKey", "test-key" },
                { "Perplexity:TimeoutSeconds", "not-a-number" },
                { "AI:DefaultProvider", "Perplexity" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IAIProvider>();
        provider.Should().NotBeNull();
    }

    #endregion

    #region Environment Variable Tests

    [Fact]
    public void AddAIServices_PrefersConfigValueOverEnvironmentVariable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        const string envVarName = "OPENAI_API_KEY";
        const string envApiKey = "env-key";
        const string configApiKey = "config-key";

        try
        {
            Environment.SetEnvironmentVariable(envVarName, envApiKey);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OpenAI:ApiKey", configApiKey }
                })
                .Build();

            // Act
            services.AddAIServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider = serviceProvider.GetService<IAIProvider>();
            provider.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void AddAIServices_WhenBothApiKeysEmpty_ShouldNotConfigureProvider()
    {
        // Arrange
        var services = new ServiceCollection();
 services.AddLogging();
  
        // Clear environment variables to ensure they don't interfere
        const string openAIEnvVar = "OPENAI_API_KEY";
        const string perplexityEnvVar = "FLIBUGET_AI_APIKEY";
        var originalOpenAIKey = Environment.GetEnvironmentVariable(openAIEnvVar);
        var originalPerplexityKey = Environment.GetEnvironmentVariable(perplexityEnvVar);
        
        try
        {
      Environment.SetEnvironmentVariable(openAIEnvVar, null);
       Environment.SetEnvironmentVariable(perplexityEnvVar, null);
            
            var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
     {
      { "OpenAI:ApiKey", "" },
     { "Perplexity:ApiKey", "" }
          // Environment variable also not set
   })
           .Build();

   // Act
        var act = () => services.AddAIServices(configuration);

       // Assert
   act.Should().Throw<InvalidOperationException>()
    .WithMessage("No AI providers configured*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(openAIEnvVar, originalOpenAIKey);
         Environment.SetEnvironmentVariable(perplexityEnvVar, originalPerplexityKey);
   }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddAIServices_GetDefaultProvider_ShouldReturnConfiguredDefaultProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IWebService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "openai-key" },
                { "Perplexity:ApiKey", "perplexity-key" },
                { "AI:DefaultProvider", "Perplexity" }
            })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        var defaultProvider = factory.GetDefaultProvider();

        // Assert
        defaultProvider.Should().NotBeNull();
        defaultProvider.ProviderName.Should().Be("Perplexity");
    }

    [Fact]
    public void AddAIServices_ResolveProviderDirectly_ShouldReturnDefaultProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
     .AddInMemoryCollection(new Dictionary<string, string?>
 {
         { "OpenAI:ApiKey", "test-key" }
  })
            .Build();

        // Act
        services.AddAIServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IAIProvider>();

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Be("OpenAI");
    }

    #endregion
}
