using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using flibuget.ViewModels;
using flibuget.Core.InfraServices;
using flibuget.Core.InfraServices.AudioTags;
using flibuget.Core.DomainServices;

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

    [Fact]
    public void ConfigureServices_ShouldRegisterDbConnectionFactory()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var factory = _serviceProvider.GetService<IDbConnectionFactory>();

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_DbConnectionFactory_ShouldBeSingleton()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var f1 = _serviceProvider.GetService<IDbConnectionFactory>();
        var f2 = _serviceProvider.GetService<IDbConnectionFactory>();

        // Assert
        f1.Should().BeSameAs(f2);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterUnitOfWork()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        using var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

        // Assert
        uow.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_UnitOfWork_ShouldBeScoped()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();
        var uow1a = scope1.ServiceProvider.GetService<IUnitOfWork>();
        var uow1b = scope1.ServiceProvider.GetService<IUnitOfWork>();
        var uow2 = scope2.ServiceProvider.GetService<IUnitOfWork>();

        // Assert
        uow1a.Should().BeSameAs(uow1b); // same scope => same instance
        uow1a.Should().NotBeSameAs(uow2); // different scopes => different instance
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterWebService()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var webService = _serviceProvider.GetService<IWebService>();

        // Assert
        webService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_WebService_ShouldBeTransient()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var w1 = _serviceProvider.GetService<IWebService>();
        var w2 = _serviceProvider.GetService<IWebService>();

        // Assert
        w1.Should().NotBeSameAs(w2);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterAudioTagService()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var tagService = _serviceProvider.GetService<IAudioTagService>();

        // Assert
        tagService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_AudioTagService_ShouldBeSingleton()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var t1 = _serviceProvider.GetService<IAudioTagService>();
        var t2 = _serviceProvider.GetService<IAudioTagService>();

        // Assert
        t1.Should().BeSameAs(t2);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterAudiobookService()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        using var scope = _serviceProvider.CreateScope();
        var svc = scope.ServiceProvider.GetService<AudiobookService>();

        // Assert
        svc.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_AudiobookService_ShouldBeScoped()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();
        var s1a = scope1.ServiceProvider.GetService<AudiobookService>();
        var s1b = scope1.ServiceProvider.GetService<AudiobookService>();
        var s2 = scope2.ServiceProvider.GetService<AudiobookService>();

        // Assert
        s1a.Should().BeSameAs(s1b);
        s1a.Should().NotBeSameAs(s2);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterHttpClientFactory()
    {
        // Act
        _serviceProvider = CompositionRoot.ConfigureServices();
        var httpFactory = _serviceProvider.GetService<IHttpClientFactory>();

        // Assert
        httpFactory.Should().NotBeNull();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
