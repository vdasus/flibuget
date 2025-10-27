using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using flibuget.ViewModels;
using Xunit;

namespace flibuget.Tests;

public class CompositionRootTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    [Fact]
    public void ConfigureServices_ShouldReturnServiceProvider()
    {
        // Act
  _serviceProvider = CompositionRoot.ConfigureServices();

        // Assert
        _serviceProvider.Should().NotBeNull();
  }

    [Fact]
 public void ConfigureServices_ShouldRegisterConfiguration()
    {
        // Act
    _serviceProvider = CompositionRoot.ConfigureServices();

        // Assert
   var configuration = _serviceProvider.GetService<IConfiguration>();
    configuration.Should().NotBeNull();
  }

    [Fact]
    public void ConfigureServices_ShouldRegisterLogger()
    {
   // Act
        _serviceProvider = CompositionRoot.ConfigureServices();

   // Assert
        var logger = _serviceProvider.GetService<ILogger<MainViewModel>>();
      logger.Should().NotBeNull();
  }

    [Fact]
    public void ConfigureServices_ShouldRegisterMainViewModel()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();

      // Assert
        var viewModel = _serviceProvider.GetService<MainViewModel>();
        viewModel.Should().NotBeNull();
 }

    [Fact]
    public void ConfigureServices_ShouldRegisterLoggerFactory()
    {
        // Act
     _serviceProvider = CompositionRoot.ConfigureServices();

     // Assert
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_MainViewModel_ShouldBeTransient()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var viewModel1 = _serviceProvider.GetService<MainViewModel>();
        var viewModel2 = _serviceProvider.GetService<MainViewModel>();

   // Assert
        viewModel1.Should().NotBeSameAs(viewModel2);
    }

    [Fact]
    public void ConfigureServices_Configuration_ShouldBeSingleton()
    {
     // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var config1 = _serviceProvider.GetService<IConfiguration>();
        var config2 = _serviceProvider.GetService<IConfiguration>();

  // Assert
        config1.Should().BeSameAs(config2);
    }

  public void Dispose()
  {
        _serviceProvider?.Dispose();
    }
}
